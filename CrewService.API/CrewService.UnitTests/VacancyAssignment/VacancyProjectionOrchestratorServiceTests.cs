using CrewService.Application.VacancyAssignment;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.VacancyAssignment;

public sealed class VacancyProjectionOrchestratorServiceTests
{
    [Fact]
    public async Task ReconcileForShiftAsync_OrdersVacanciesByAssignmentCodeThenDisplayOrder()
    {
        var fixture = Fixture.Create(
            slotSpecs:
            [
                new SlotSpec("001", 2, MarkedOff: true),
                new SlotSpec("001", 1, MarkedOff: true),
                new SlotSpec("002", 1, MarkedOff: true)
            ],
            candidateEmployeeCtrlNbrs: [701, 702, 703],
            restedEmployeeCtrlNbrs: new HashSet<long> { 701, 702, 703 });

        await fixture.Sut.ReconcileForShiftsAsync(fixture.Uow, fixture.WorkAreaCtrlNbr, [fixture.Shift], TestContext.Current.CancellationToken);

        var ordered = fixture.Shift.PositionSlots
            .OrderBy(s => s.AssignmentCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.DisplayOrder)
            .Select(s => s.CtrlNbr)
            .ToList();

        AssertProjectionEmployee(fixture.ProjectionRepo, ordered[0], 701);
        AssertProjectionEmployee(fixture.ProjectionRepo, ordered[1], 702);
        AssertProjectionEmployee(fixture.ProjectionRepo, ordered[2], 703);
    }

    [Fact]
    public async Task ReconcileForShiftAsync_WrapsRestedCandidatesWhenVacanciesExceedCandidates()
    {
        var fixture = Fixture.Create(
            slotSpecs:
            [
                new SlotSpec("100", 1, MarkedOff: true),
                new SlotSpec("100", 2, MarkedOff: true),
                new SlotSpec("100", 3, MarkedOff: true)
            ],
            candidateEmployeeCtrlNbrs: [801, 802],
            restedEmployeeCtrlNbrs: new HashSet<long> { 801, 802 });

        await fixture.Sut.ReconcileForShiftsAsync(fixture.Uow, fixture.WorkAreaCtrlNbr, [fixture.Shift], TestContext.Current.CancellationToken);

        var ordered = fixture.Shift.PositionSlots.OrderBy(s => s.DisplayOrder).Select(s => s.CtrlNbr).ToList();
        AssertProjectionEmployee(fixture.ProjectionRepo, ordered[0], 801);
        AssertProjectionEmployee(fixture.ProjectionRepo, ordered[1], 802);
        var thirdProjection = fixture.ProjectionRepo.Seeded.Single(p => p.PositionSlotCtrlNbr == ordered[2]);
        Assert.Null(thirdProjection.ProjectedEmployeeCtrlNbr);
    }

    [Fact]
    public async Task ReconcileForShiftAsync_WhenNoRestedCandidates_ProjectsNullEmployee()
    {
        var fixture = Fixture.Create(
            slotSpecs: [new SlotSpec("200", 1, MarkedOff: true)],
            candidateEmployeeCtrlNbrs: [901],
            restedEmployeeCtrlNbrs: new HashSet<long>());

        await fixture.Sut.ReconcileForShiftsAsync(fixture.Uow, fixture.WorkAreaCtrlNbr, [fixture.Shift], TestContext.Current.CancellationToken);

        var projection = Assert.Single(fixture.ProjectionRepo.Seeded);
        Assert.Null(projection.ProjectedEmployeeCtrlNbr);
    }

    [Fact]
    public async Task ReconcileForShiftAsync_CreatesThenAbolishesAbsenceVacancyAndClearsProjection()
    {
        var fixture = Fixture.Create(
            slotSpecs: [new SlotSpec("300", 1, MarkedOff: true)],
            candidateEmployeeCtrlNbrs: [1001],
            restedEmployeeCtrlNbrs: new HashSet<long> { 1001 });

        var slot = Assert.Single(fixture.Shift.PositionSlots);

        await fixture.Sut.ReconcileForShiftsAsync(fixture.Uow, fixture.WorkAreaCtrlNbr, [fixture.Shift], TestContext.Current.CancellationToken);

        var openVacancy = Assert.Single(fixture.VacancyRepo.Seeded);
        Assert.Equal("Open", openVacancy.Status);
        Assert.Single(fixture.ProjectionRepo.Seeded);

        slot.ClearMarkedOff();

        await fixture.Sut.ReconcileForShiftsAsync(fixture.Uow, fixture.WorkAreaCtrlNbr, [fixture.Shift], TestContext.Current.CancellationToken);

        Assert.Equal("Abolished", openVacancy.Status);
        Assert.Empty(fixture.ProjectionRepo.Seeded);
    }

    [Fact]
    public async Task ReconcileForShiftAsync_IsIdempotentForOpenVacancies()
    {
        var fixture = Fixture.Create(
            slotSpecs: [new SlotSpec("400", 1, MarkedOff: true)],
            candidateEmployeeCtrlNbrs: [1101],
            restedEmployeeCtrlNbrs: new HashSet<long> { 1101 });

        await fixture.Sut.ReconcileForShiftsAsync(fixture.Uow, fixture.WorkAreaCtrlNbr, [fixture.Shift], TestContext.Current.CancellationToken);
        await fixture.Sut.ReconcileForShiftsAsync(fixture.Uow, fixture.WorkAreaCtrlNbr, [fixture.Shift], TestContext.Current.CancellationToken);

        Assert.Single(fixture.VacancyRepo.Seeded, v => v.Status == "Open");
        Assert.Single(fixture.ProjectionRepo.Seeded);
    }

    [Fact]
    public async Task ReconcileForShiftAsync_UsesNumericAssignmentCodeOrdering()
    {
        var fixture = Fixture.Create(
            slotSpecs:
            [
                new SlotSpec("2", 1, MarkedOff: true),
                new SlotSpec("10", 1, MarkedOff: true)
            ],
            candidateEmployeeCtrlNbrs: [1201, 1202],
            restedEmployeeCtrlNbrs: new HashSet<long> { 1201, 1202 });

        await fixture.Sut.ReconcileForShiftsAsync(fixture.Uow, fixture.WorkAreaCtrlNbr, [fixture.Shift], TestContext.Current.CancellationToken);

        var slot10 = fixture.Shift.PositionSlots.Single(s => s.AssignmentCode == "10");
        var slot2 = fixture.Shift.PositionSlots.Single(s => s.AssignmentCode == "2");

        AssertProjectionEmployee(fixture.ProjectionRepo, slot2.CtrlNbr, 1201);
        AssertProjectionEmployee(fixture.ProjectionRepo, slot10.CtrlNbr, 1202);
    }

    [Fact]
    public async Task ReconcileForShiftAsync_UsesNumericPositionDisplayOrdering()
    {
        var fixture = Fixture.Create(
            slotSpecs:
            [
                new SlotSpec("500", 10, MarkedOff: true),
                new SlotSpec("500", 2, MarkedOff: true)
            ],
            candidateEmployeeCtrlNbrs: [1301, 1302],
            restedEmployeeCtrlNbrs: new HashSet<long> { 1301, 1302 });

        await fixture.Sut.ReconcileForShiftsAsync(fixture.Uow, fixture.WorkAreaCtrlNbr, [fixture.Shift], TestContext.Current.CancellationToken);

        var slot2 = fixture.Shift.PositionSlots.Single(s => s.DisplayOrder == 2);
        var slot10 = fixture.Shift.PositionSlots.Single(s => s.DisplayOrder == 10);

        AssertProjectionEmployee(fixture.ProjectionRepo, slot2.CtrlNbr, 1301);
        AssertProjectionEmployee(fixture.ProjectionRepo, slot10.CtrlNbr, 1302);
    }

    [Fact]
    public async Task ReconcileForEmployeeAsync_ProjectsAllVacanciesInImpactedShift()
    {
        var fixture = Fixture.Create(
            slotSpecs:
            [
                new SlotSpec("230", 1, MarkedOff: true),
                new SlotSpec("240", 1, MarkedOff: true)
            ],
            candidateEmployeeCtrlNbrs: [1401, 1402, 1403],
            restedEmployeeCtrlNbrs: new HashSet<long> { 1401, 1402, 1403 });

        await fixture.Sut.ReconcileForEmployeeAsync(
            fixture.Uow,
            fixture.TargetEmployeeCtrlNbr,
            TestContext.Current.CancellationToken);

        var firstSlot = fixture.Shift.PositionSlots.Single(s => s.AssignmentCode == "230");
        var secondSlot = fixture.Shift.PositionSlots.Single(s => s.AssignmentCode == "240");

        AssertProjectionEmployee(fixture.ProjectionRepo, firstSlot.CtrlNbr, 1401);
        AssertProjectionEmployee(fixture.ProjectionRepo, secondSlot.CtrlNbr, 1402);
    }

    [Fact]
    public async Task ReconcileForEmployeeAsync_WithEffectiveFromUtc_ExcludesEarlierShifts()
    {
        var targetEmployeeCtrlNbr = ControlNumber.Create(9500);
        var craftCtrlNbr = ControlNumber.Create(101);
        var craftRole = CraftRole.Create(craftCtrlNbr, "H", "Helper");
        var staffablePositionCtrlNbr = ControlNumber.Create(7600);
        var crewPosition = CrewPosition.Create(ControlNumber.Create(6100), craftRole.CtrlNbr, 1, staffablePositionCtrlNbr);
        var assignment = PositionAssignment.Create(staffablePositionCtrlNbr, targetEmployeeCtrlNbr, PositionAssignmentType.Direct);

        var workAreaCtrlNbr = ControlNumber.Create(4100);
        var day1 = DateTime.SpecifyKind(new DateTime(2026, 7, 28, 15, 0, 0), DateTimeKind.Utc);
        var day2 = DateTime.SpecifyKind(new DateTime(2026, 7, 29, 15, 0, 0), DateTimeKind.Utc);
        var day3 = DateTime.SpecifyKind(new DateTime(2026, 7, 30, 15, 0, 0), DateTimeKind.Utc);

        var contexts = new List<(WorkInstance WorkInstance, ShiftInstance Shift, PositionSlotInstance Slot)>();
        foreach (var start in new[] { day1, day2, day3 })
        {
            var workInstance = WorkInstance.Create(null, workAreaCtrlNbr, start, start.AddHours(8), null);
            var shift = ShiftInstance.Create(workInstance.CtrlNbr, ControlNumber.Create(), "DAY", "Day Shift");
            var slot = shift.AddPositionSlot(
                crewPosition.CtrlNbr,
                targetEmployeeCtrlNbr,
                1,
                ControlNumber.Create(),
                ((int)(start.Day)).ToString(),
                "Assignment",
                craftRole.Name,
                "Group",
                "GRP",
                new TimeOnly(7, 0),
                new TimeOnly(15, 0));
            slot.MarkMarkedOff();
            contexts.Add((workInstance, shift, slot));
        }

        var shiftRepo = new FakeShiftInstanceRepository(contexts.Select(c => c.Shift).ToList());
        var workRepo = new FakeWorkInstanceRepository(contexts.Select(c => c.WorkInstance).ToList());

        var assignmentRepo = new FakePositionAssignmentRepository([assignment]);
        var crewPositionRepo = new FakeCrewPositionRepository([crewPosition]);
        var craftRoleRepo = new FakeCraftRoleRepository([craftRole]);
        var vacancyRepo = new FakePositionVacancyRepository();
        var projectionRepo = new FakeDispatchProjectionRepository();

        var uow = new FakeOrchestrationUow(
            shiftRepo,
            workRepo,
            crewPositionRepo,
            assignmentRepo,
            craftRoleRepo,
            vacancyRepo,
            projectionRepo);

        var candidateProvider = new FakeBoardCandidateProvider(
            [new SkipRuleCandidate(ControlNumber.Create(1501), ControlNumber.Create(15001), 1)]);
        var skipContextProvider = new FakeSkipContextProvider(new HashSet<long> { 1501 });
        var sut = new VacancyProjectionOrchestratorService(candidateProvider, skipContextProvider);

        await sut.ReconcileForEmployeeAsync(uow, targetEmployeeCtrlNbr, day2, TestContext.Current.CancellationToken);

        var day1Slot = contexts[0].Slot;
        var day2Slot = contexts[1].Slot;
        var day3Slot = contexts[2].Slot;

        Assert.DoesNotContain(projectionRepo.Seeded, p => p.PositionSlotCtrlNbr == day1Slot.CtrlNbr);
        Assert.Contains(projectionRepo.Seeded, p => p.PositionSlotCtrlNbr == day2Slot.CtrlNbr);
        Assert.Contains(projectionRepo.Seeded, p => p.PositionSlotCtrlNbr == day3Slot.CtrlNbr);
    }

    [Fact]
    public async Task ReconcileForEmployeeAsync_RebuildsAllIncompleteShiftsInImpactedWorkArea()
    {
        var targetEmployeeCtrlNbr = ControlNumber.Create(9600);
        var otherEmployeeCtrlNbr = ControlNumber.Create(9601);
        var craftCtrlNbr = ControlNumber.Create(111);
        var craftRole = CraftRole.Create(craftCtrlNbr, "F", "Foreman");

        var impactedWorkAreaCtrlNbr = ControlNumber.Create(4200);
        var otherWorkAreaCtrlNbr = ControlNumber.Create(4300);
        var start = DateTime.SpecifyKind(new DateTime(2026, 8, 1, 12, 0, 0), DateTimeKind.Utc);

        var workInstance1 = WorkInstance.Create(null, impactedWorkAreaCtrlNbr, start, start.AddHours(8), null);
        var shift1 = ShiftInstance.Create(workInstance1.CtrlNbr, ControlNumber.Create(5100), "1", "First Shift");
        var targetStaffablePosition = ControlNumber.Create(7700);
        var targetCrewPosition = CrewPosition.Create(ControlNumber.Create(6200), craftRole.CtrlNbr, 1, targetStaffablePosition);
        var impactedSlot = shift1.AddPositionSlot(
            targetCrewPosition.CtrlNbr,
            targetEmployeeCtrlNbr,
            1,
            ControlNumber.Create(8400),
            "110",
            "Assignment 110",
            craftRole.Name,
            "Group",
            "GRP",
            new TimeOnly(7, 0),
            new TimeOnly(15, 0));
        impactedSlot.MarkMarkedOff();

        var workInstance2 = WorkInstance.Create(null, impactedWorkAreaCtrlNbr, start.AddDays(1), start.AddDays(1).AddHours(8), null);
        var shift2 = ShiftInstance.Create(workInstance2.CtrlNbr, ControlNumber.Create(5101), "2", "Second Shift");
        var otherStaffablePosition = ControlNumber.Create(7701);
        var otherCrewPosition = CrewPosition.Create(ControlNumber.Create(6201), craftRole.CtrlNbr, 2, otherStaffablePosition);
        var sameAreaSlot = shift2.AddPositionSlot(
            otherCrewPosition.CtrlNbr,
            otherEmployeeCtrlNbr,
            1,
            ControlNumber.Create(8401),
            "120",
            "Assignment 120",
            craftRole.Name,
            "Group",
            "GRP",
            new TimeOnly(7, 0),
            new TimeOnly(15, 0));
        sameAreaSlot.MarkMarkedOff();

        var workInstance3 = WorkInstance.Create(null, otherWorkAreaCtrlNbr, start.AddDays(1), start.AddDays(1).AddHours(8), null);
        var shift3 = ShiftInstance.Create(workInstance3.CtrlNbr, ControlNumber.Create(5102), "1", "Other Area Shift");
        var outsideAreaSlot = shift3.AddPositionSlot(
            otherCrewPosition.CtrlNbr,
            otherEmployeeCtrlNbr,
            1,
            ControlNumber.Create(8402),
            "130",
            "Assignment 130",
            craftRole.Name,
            "Group",
            "GRP",
            new TimeOnly(7, 0),
            new TimeOnly(15, 0));
        outsideAreaSlot.MarkMarkedOff();

        var shiftRepo = new FakeShiftInstanceRepository(
            [shift1, shift2, shift3],
            new Dictionary<ControlNumber, ControlNumber>
            {
                [workInstance1.CtrlNbr] = impactedWorkAreaCtrlNbr,
                [workInstance2.CtrlNbr] = impactedWorkAreaCtrlNbr,
                [workInstance3.CtrlNbr] = otherWorkAreaCtrlNbr
            });
        var workRepo = new FakeWorkInstanceRepository([workInstance1, workInstance2, workInstance3]);
        var assignmentRepo = new FakePositionAssignmentRepository([PositionAssignment.Create(targetStaffablePosition, targetEmployeeCtrlNbr, PositionAssignmentType.Direct)]);
        var crewPositionRepo = new FakeCrewPositionRepository([targetCrewPosition, otherCrewPosition]);
        var craftRoleRepo = new FakeCraftRoleRepository([craftRole]);
        var vacancyRepo = new FakePositionVacancyRepository();
        var projectionRepo = new FakeDispatchProjectionRepository();

        var uow = new FakeOrchestrationUow(
            shiftRepo,
            workRepo,
            crewPositionRepo,
            assignmentRepo,
            craftRoleRepo,
            vacancyRepo,
            projectionRepo);

        var candidateProvider = new FakeBoardCandidateProvider(
            [new SkipRuleCandidate(ControlNumber.Create(1801), ControlNumber.Create(18001), 1)]);
        var skipContextProvider = new FakeSkipContextProvider(new HashSet<long> { 1801 });
        var sut = new VacancyProjectionOrchestratorService(candidateProvider, skipContextProvider);

        await sut.ReconcileForEmployeeAsync(uow, targetEmployeeCtrlNbr, TestContext.Current.CancellationToken);

        Assert.Contains(projectionRepo.Seeded, p => p.PositionSlotCtrlNbr == impactedSlot.CtrlNbr);
        Assert.Contains(projectionRepo.Seeded, p => p.PositionSlotCtrlNbr == sameAreaSlot.CtrlNbr);
        Assert.DoesNotContain(projectionRepo.Seeded, p => p.PositionSlotCtrlNbr == outsideAreaSlot.CtrlNbr);
    }

    [Fact]
    public async Task ReconcileForEmployeeAsync_UsesEmployeeOnDutySlotWhenIncumbentNoLongerMatches()
    {
        var targetEmployeeCtrlNbr = ControlNumber.Create(9900);
        var replacementEmployeeCtrlNbr = ControlNumber.Create(9901);
        var craftCtrlNbr = ControlNumber.Create(121);
        var craftRole = CraftRole.Create(craftCtrlNbr, "H", "Helper");

        var workAreaCtrlNbr = ControlNumber.Create(4400);
        var start = DateTime.SpecifyKind(new DateTime(2026, 8, 8, 0, 0, 0), DateTimeKind.Utc);

        var workInstance = WorkInstance.Create(null, workAreaCtrlNbr, start, start.AddDays(1), null);
        var shift = ShiftInstance.Create(workInstance.CtrlNbr, ControlNumber.Create(5200), "3", "Third Shift");

        var staffablePositionCtrlNbr = ControlNumber.Create(8800);
        var crewPosition = CrewPosition.Create(ControlNumber.Create(6800), craftRole.CtrlNbr, 1, staffablePositionCtrlNbr);

        var slot = shift.AddPositionSlot(
            crewPosition.CtrlNbr,
            replacementEmployeeCtrlNbr,
            1,
            ControlNumber.Create(8600),
            "350",
            "Assignment 350",
            craftRole.Name,
            "Group",
            "GRP",
            new TimeOnly(23, 59),
            new TimeOnly(7, 59));
        slot.MarkMarkedOff();

        var shiftRepo = new FakeShiftInstanceRepository([shift]);
        var workRepo = new FakeWorkInstanceRepository([workInstance]);
        var assignmentRepo = new FakePositionAssignmentRepository([PositionAssignment.Create(staffablePositionCtrlNbr, targetEmployeeCtrlNbr, PositionAssignmentType.Direct)]);
        var crewPositionRepo = new FakeCrewPositionRepository([crewPosition]);
        var craftRoleRepo = new FakeCraftRoleRepository([craftRole]);
        var vacancyRepo = new FakePositionVacancyRepository();
        var projectionRepo = new FakeDispatchProjectionRepository();
        var onDutyRepo = new FakeOnDutyRecordRepository(
            [
                OnDutyRecord.CreateScheduled(
                    slot.CtrlNbr,
                    targetEmployeeCtrlNbr,
                    DateTime.SpecifyKind(new DateTime(2026, 8, 8, 4, 59, 0), DateTimeKind.Utc),
                    10m,
                    0,
                    0,
                    isAssigned: true)
            ]);

        var uow = new FakeOrchestrationUow(
            shiftRepo,
            workRepo,
            crewPositionRepo,
            assignmentRepo,
            craftRoleRepo,
            vacancyRepo,
            projectionRepo,
            onDutyRepo);

        var candidateProvider = new FakeBoardCandidateProvider(
            [new SkipRuleCandidate(ControlNumber.Create(1950), ControlNumber.Create(19500), 1)]);
        var skipContextProvider = new FakeSkipContextProvider(new HashSet<long> { 1950 });
        var sut = new VacancyProjectionOrchestratorService(candidateProvider, skipContextProvider);

        await sut.ReconcileForEmployeeAsync(
            uow,
            targetEmployeeCtrlNbr,
            DateTime.SpecifyKind(new DateTime(2026, 8, 8, 0, 2, 0), DateTimeKind.Utc),
            TestContext.Current.CancellationToken);

        Assert.Contains(projectionRepo.Seeded, p => p.PositionSlotCtrlNbr == slot.CtrlNbr);
    }

    [Fact]
    public async Task ReconcileForShiftAsync_WithSharedSequence_AdvancesAcrossShifts()
    {
        var fixture = Fixture.Create(
            slotSpecs: [new SlotSpec("230", 1, MarkedOff: true)],
            candidateEmployeeCtrlNbrs: [1601, 1602],
            restedEmployeeCtrlNbrs: new HashSet<long> { 1601, 1602 });

        var secondShift = ShiftInstance.Create(fixture.Shift.WorkInstanceCtrlNbr, ControlNumber.Create(5001), "2", "Second Shift");
        var crewPosition = fixture.CrewPositions[0];
        secondShift.AddPositionSlot(
            crewPosition.CtrlNbr,
            fixture.TargetEmployeeCtrlNbr,
            1,
            ControlNumber.Create(8100),
            "330",
            "Assignment 330",
            "Foreman",
            "Group",
            "GRP",
            new TimeOnly(15, 0),
            new TimeOnly(23, 0));
        secondShift.PositionSlots[0].MarkMarkedOff();

        await fixture.Sut.ReconcileForShiftsAsync(
            fixture.Uow,
            fixture.WorkAreaCtrlNbr,
            [fixture.Shift, secondShift],
            TestContext.Current.CancellationToken);

        var firstProjection = fixture.ProjectionRepo.Seeded.Single(p => p.PositionSlotCtrlNbr == fixture.Shift.PositionSlots[0].CtrlNbr);
        var secondProjection = fixture.ProjectionRepo.Seeded.Single(p => p.PositionSlotCtrlNbr == secondShift.PositionSlots[0].CtrlNbr);

        Assert.Equal(1601, firstProjection.ProjectedEmployeeCtrlNbr!.Value);
        Assert.Equal(1602, secondProjection.ProjectedEmployeeCtrlNbr!.Value);
    }

    [Fact]
    public async Task ReconcileForShiftsAsync_GathersAllVacanciesAndProjectsInGlobalOrder()
    {
        var fixture = Fixture.Create(
            slotSpecs: [new SlotSpec("240", 1, MarkedOff: true)],
            candidateEmployeeCtrlNbrs: [1701, 1702],
            restedEmployeeCtrlNbrs: new HashSet<long> { 1701, 1702 });

        var crewPosition = fixture.CrewPositions[0];
        var secondShift = ShiftInstance.Create(fixture.Shift.WorkInstanceCtrlNbr, ControlNumber.Create(5002), "2", "Second Shift");
        secondShift.AddPositionSlot(
            crewPosition.CtrlNbr,
            fixture.TargetEmployeeCtrlNbr,
            1,
            ControlNumber.Create(8200),
            "240",
            "Assignment 240",
            "Foreman",
            "Group",
            "GRP",
            new TimeOnly(15, 0),
            new TimeOnly(23, 0));
        secondShift.PositionSlots[0].MarkMarkedOff();

        var thirdShift = ShiftInstance.Create(fixture.Shift.WorkInstanceCtrlNbr, ControlNumber.Create(5003), "3", "Third Shift");
        thirdShift.AddPositionSlot(
            crewPosition.CtrlNbr,
            fixture.TargetEmployeeCtrlNbr,
            1,
            ControlNumber.Create(8300),
            "330",
            "Assignment 330",
            "Helper",
            "Group",
            "GRP",
            new TimeOnly(23, 0),
            new TimeOnly(7, 0));
        thirdShift.PositionSlots[0].MarkMarkedOff();

        await fixture.Sut.ReconcileForShiftsAsync(
            fixture.Uow,
            fixture.WorkAreaCtrlNbr,
            [secondShift, thirdShift],
            TestContext.Current.CancellationToken);

        var secondProjection = fixture.ProjectionRepo.Seeded.Single(p => p.PositionSlotCtrlNbr == secondShift.PositionSlots[0].CtrlNbr);
        var thirdProjection = fixture.ProjectionRepo.Seeded.Single(p => p.PositionSlotCtrlNbr == thirdShift.PositionSlots[0].CtrlNbr);

        Assert.Equal(1701, secondProjection.ProjectedEmployeeCtrlNbr!.Value);
        Assert.Equal(1702, thirdProjection.ProjectedEmployeeCtrlNbr!.Value);
    }

    [Fact]
    public async Task ReconcileForShiftsAsync_WithAnchorSlot_ReevaluatesAnchorForwardOnly()
    {
        var fixture = Fixture.Create(
            slotSpecs:
            [
                new SlotSpec("500", 1, MarkedOff: true),
                new SlotSpec("500", 2, MarkedOff: true),
                new SlotSpec("500", 3, MarkedOff: true)
            ],
            candidateEmployeeCtrlNbrs: [2101, 2102, 2103, 2104],
            restedEmployeeCtrlNbrs: new HashSet<long> { 2101, 2102, 2103, 2104 });

        var crewPosition = fixture.CrewPositions[0];
        var laterShift = ShiftInstance.Create(fixture.Shift.WorkInstanceCtrlNbr, ControlNumber.Create(5004), "2", "Second Shift");
        var laterSlot = laterShift.AddPositionSlot(
            crewPosition.CtrlNbr,
            fixture.TargetEmployeeCtrlNbr,
            1,
            ControlNumber.Create(8400),
            "600",
            "Assignment 600",
            "Foreman",
            "Group",
            "GRP",
            new TimeOnly(15, 0),
            new TimeOnly(23, 0));
        laterSlot.MarkMarkedOff();

        var anchorSlot = fixture.Shift.PositionSlots.Single(s => s.DisplayOrder == 2);

        await fixture.Sut.ReconcileForShiftsAsync(
            fixture.Uow,
            fixture.WorkAreaCtrlNbr,
            [fixture.Shift, laterShift],
            anchorSlot.CtrlNbr,
            TestContext.Current.CancellationToken);

        var firstSlot = fixture.Shift.PositionSlots.Single(s => s.DisplayOrder == 1);
        var thirdSlot = fixture.Shift.PositionSlots.Single(s => s.DisplayOrder == 3);

        Assert.DoesNotContain(fixture.ProjectionRepo.Seeded, p => p.PositionSlotCtrlNbr == firstSlot.CtrlNbr);

        var anchorProjection = fixture.ProjectionRepo.Seeded.Single(p => p.PositionSlotCtrlNbr == anchorSlot.CtrlNbr);
        var thirdProjection = fixture.ProjectionRepo.Seeded.Single(p => p.PositionSlotCtrlNbr == thirdSlot.CtrlNbr);
        var laterProjection = fixture.ProjectionRepo.Seeded.Single(p => p.PositionSlotCtrlNbr == laterSlot.CtrlNbr);

        Assert.Equal(2101, anchorProjection.ProjectedEmployeeCtrlNbr!.Value);
        Assert.Equal(2102, thirdProjection.ProjectedEmployeeCtrlNbr!.Value);
        Assert.Equal(2103, laterProjection.ProjectedEmployeeCtrlNbr!.Value);

        var projectedEmployees = fixture.ProjectionRepo.Seeded
            .Where(p => p.ProjectedEmployeeCtrlNbr is not null)
            .Select(p => p.ProjectedEmployeeCtrlNbr!.Value)
            .ToList();

        Assert.Equal(projectedEmployees.Count, projectedEmployees.Distinct().Count());
    }

    private static void AssertProjectionEmployee(FakeDispatchProjectionRepository repo, ControlNumber slotCtrlNbr, long expectedEmployeeCtrlNbr)
    {
        var projection = repo.Seeded.Single(p => p.PositionSlotCtrlNbr == slotCtrlNbr);
        Assert.Equal(expectedEmployeeCtrlNbr, projection.ProjectedEmployeeCtrlNbr!.Value);
    }

    private sealed record SlotSpec(string AssignmentCode, int DisplayOrder, bool MarkedOff);

    private sealed class Fixture
    {
        public required VacancyProjectionOrchestratorService Sut { get; init; }
        public required FakeOrchestrationUow Uow { get; init; }
        public required ShiftInstance Shift { get; init; }
        public required FakeDispatchProjectionRepository ProjectionRepo { get; init; }
        public required FakePositionVacancyRepository VacancyRepo { get; init; }
        public required ControlNumber WorkAreaCtrlNbr { get; init; }
        public required ControlNumber TargetEmployeeCtrlNbr { get; init; }
        public required IReadOnlyList<CrewPosition> CrewPositions { get; init; }

        public static Fixture Create(
            IReadOnlyList<SlotSpec> slotSpecs,
            IReadOnlyList<long> candidateEmployeeCtrlNbrs,
            IReadOnlySet<long> restedEmployeeCtrlNbrs)
        {
            var workAreaCtrlNbr = ControlNumber.Create(4000);
            var workInstance = WorkInstance.Create(null, workAreaCtrlNbr, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddHours(12), null);

            var shift = ShiftInstance.Create(workInstance.CtrlNbr, ControlNumber.Create(5000), "DAY", "Day Shift");
            var targetEmployeeCtrlNbr = ControlNumber.Create(9000);
            var craftCtrlNbr = ControlNumber.Create(100);
            var craftRole = CraftRole.Create(craftCtrlNbr, "F", "Foreman");

            var crewPositions = new List<CrewPosition>();
            var assignments = new List<PositionAssignment>();

            for (var i = 0; i < slotSpecs.Count; i++)
            {
                var staffablePositionCtrlNbr = ControlNumber.Create(7000 + i);
                var crewPosition = CrewPosition.Create(ControlNumber.Create(6000), craftRole.CtrlNbr, i + 1, staffablePositionCtrlNbr);
                crewPositions.Add(crewPosition);
                assignments.Add(PositionAssignment.Create(staffablePositionCtrlNbr, targetEmployeeCtrlNbr, PositionAssignmentType.Direct));

                var slot = shift.AddPositionSlot(
                    crewPosition.CtrlNbr,
                    targetEmployeeCtrlNbr,
                    slotSpecs[i].DisplayOrder,
                    ControlNumber.Create(8000 + i),
                    slotSpecs[i].AssignmentCode,
                    $"Assignment {slotSpecs[i].AssignmentCode}",
                    craftRole.Name,
                    "Group",
                    "GRP",
                    new TimeOnly(7, 0),
                    new TimeOnly(15, 0));

                if (slotSpecs[i].MarkedOff)
                    slot.MarkMarkedOff();
            }

            var shiftRepo = new FakeShiftInstanceRepository([shift]);
            var workInstanceRepo = new FakeWorkInstanceRepository([workInstance]);
            var crewPositionRepo = new FakeCrewPositionRepository(crewPositions);
            var assignmentRepo = new FakePositionAssignmentRepository(assignments);
            var craftRoleRepo = new FakeCraftRoleRepository([craftRole]);
            var vacancyRepo = new FakePositionVacancyRepository();
            var projectionRepo = new FakeDispatchProjectionRepository();

            var uow = new FakeOrchestrationUow(
                shiftRepo,
                workInstanceRepo,
                crewPositionRepo,
                assignmentRepo,
                craftRoleRepo,
                vacancyRepo,
                projectionRepo);

            var candidates = candidateEmployeeCtrlNbrs
                .Select((value, idx) => new SkipRuleCandidate(ControlNumber.Create(value), ControlNumber.Create(10000 + idx), idx + 1))
                .ToList();

            var candidateProvider = new FakeBoardCandidateProvider(candidates);
            var skipContextProvider = new FakeSkipContextProvider(restedEmployeeCtrlNbrs);

            var sut = new VacancyProjectionOrchestratorService(candidateProvider, skipContextProvider);

            return new Fixture
            {
                Sut = sut,
                Uow = uow,
                Shift = shift,
                ProjectionRepo = projectionRepo,
                VacancyRepo = vacancyRepo,
                WorkAreaCtrlNbr = workAreaCtrlNbr,
                TargetEmployeeCtrlNbr = targetEmployeeCtrlNbr,
                CrewPositions = crewPositions
            };
        }
    }

    private sealed class FakeBoardCandidateProvider(IReadOnlyList<SkipRuleCandidate> candidates) : IBoardCandidateProvider
    {
        public Task<IReadOnlyList<SkipRuleCandidate>> GetCandidatesAsync(
            ControlNumber workAreaGroupCtrlNbr,
            ControlNumber craftCtrlNbr,
            SkipRuleSlot slot,
            CancellationToken ct = default)
            => Task.FromResult(candidates);
    }

    private sealed class FakeSkipContextProvider(IReadOnlySet<long> restedEmployeeCtrlNbrs) : ISkipContextProvider
    {
        public Task<SkipContext> BuildAsync(IOrchestrationUnitOfWork uow, SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default)
            => BuildAsync(candidate, slot, ct);

        public Task<SkipContext> BuildAsync(SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default)
        {
            var isRested = restedEmployeeCtrlNbrs.Contains(candidate.EmployeeCtrlNbr.Value);
            return Task.FromResult(new SkipContext
            {
                IsRested = isRested,
                IsQualified = true,
                HasActiveOnDuty = false,
                RestedAtUtc = isRested
                    ? DateTime.SpecifyKind(DateTime.UnixEpoch, DateTimeKind.Utc)
                    : DateTime.SpecifyKind(new DateTime(9999, 12, 31, 0, 0, 0), DateTimeKind.Utc)
            });
        }
    }

    private sealed class FakeWorkInstanceRepository(IReadOnlyList<WorkInstance> workInstances) : FakeRepository<WorkInstance>, IWorkInstanceRepository
    {
        private readonly Dictionary<ControlNumber, WorkInstance> _workInstancesByCtrlNbr = workInstances.ToDictionary(w => w.CtrlNbr);

        public override Task<WorkInstance?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(_workInstancesByCtrlNbr.GetValueOrDefault(ctrlNbr));

        public Task<List<WorkInstance>> GetByWorkAreaAndDateRangeAsync(ControlNumber workAreaGroupCtrlNbr, DateTime startUtc, DateTime endUtc)
            => Task.FromResult(_workInstancesByCtrlNbr.Values
                .Where(w => w.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && w.StartUtc < endUtc && w.EndUtc > startUtc)
                .ToList());
    }

    private sealed class FakeShiftInstanceRepository(
        IReadOnlyList<ShiftInstance> shifts,
        IReadOnlyDictionary<ControlNumber, ControlNumber>? workAreaByWorkInstanceCtrlNbr = null)
        : FakeRepository<ShiftInstance>, IShiftInstanceRepository
    {
        public Task<IReadOnlyList<ShiftInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShiftInstance>>(shifts.Where(s => s.WorkInstanceCtrlNbr == workInstanceCtrlNbr).ToList());

        public Task<bool> ExistsByWorkInstanceAndShiftCodeAsync(ControlNumber workInstanceCtrlNbr, string shiftCode, ControlNumber? departmentCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<ShiftInstance>> GetIncompleteByCrewPositionAsync(ControlNumber crewPositionCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShiftInstance>>(shifts
                .Where(s => !s.IsComplete && s.PositionSlots.Any(p => p.CrewPositionCtrlNbr == crewPositionCtrlNbr))
                .ToList());

        public Task<IReadOnlyList<ShiftInstance>> GetIncompleteByIncumbentEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShiftInstance>>(shifts
                .Where(s => !s.IsComplete && s.PositionSlots.Any(p => p.IncumbentEmployeeCtrlNbr == employeeCtrlNbr))
                .ToList());

        public Task<IReadOnlyList<ShiftInstance>> GetIncompleteByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShiftInstance>>(shifts
                .Where(s => !s.IsComplete
                    && (workAreaByWorkInstanceCtrlNbr?.TryGetValue(s.WorkInstanceCtrlNbr, out var areaCtrlNbr) != true
                        || areaCtrlNbr == workAreaGroupCtrlNbr))
                .ToList());
    }

    private sealed class FakeCrewPositionRepository(IReadOnlyList<CrewPosition> positions)
        : FakeRepository<CrewPosition>, ICrewPositionRepository
    {
        public override Task<CrewPosition?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(positions.FirstOrDefault(p => p.CtrlNbr == ctrlNbr));

        public Task<List<CrewPosition>> GetByCrewAsync(ControlNumber crewCtrlNbr) => Task.FromResult(new List<CrewPosition>());
        public Task<List<CrewPosition>> GetByCrewsAsync(IEnumerable<ControlNumber> crewCtrlNbrs) => Task.FromResult(new List<CrewPosition>());
        public Task<CrewPosition?> GetByStaffablePositionAsync(ControlNumber staffablePositionCtrlNbr)
            => Task.FromResult(positions.FirstOrDefault(p => p.StaffablePositionCtrlNbr == staffablePositionCtrlNbr));
        public Task<List<ControlNumber>> GetVacantStaffablePositionCtrlNbrsAsync(CancellationToken ct = default) => Task.FromResult(new List<ControlNumber>());
    }

    private sealed class FakePositionAssignmentRepository(IReadOnlyList<PositionAssignment> assignments)
        : FakeRepository<PositionAssignment>, IPositionAssignmentRepository
    {
        public Task<PositionAssignment?> GetByStaffablePositionAsync(ControlNumber staffablePositionCtrlNbr)
            => Task.FromResult(assignments.FirstOrDefault(a => a.StaffablePositionCtrlNbr == staffablePositionCtrlNbr));

        public Task<List<PositionAssignment>> GetByStaffablePositionsAsync(IEnumerable<ControlNumber> staffablePositionCtrlNbrs)
            => Task.FromResult(assignments.Where(a => staffablePositionCtrlNbrs.Contains(a.StaffablePositionCtrlNbr)).ToList());

        public Task<List<PositionAssignment>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr)
            => Task.FromResult(assignments.Where(a => a.EmployeeCtrlNbr == employeeCtrlNbr).ToList());

        public Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsAsync()
            => Task.FromResult(assignments.Select(a => a.EmployeeCtrlNbr.Value).ToHashSet());

        public Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsByTypeAsync(string assignmentType)
            => Task.FromResult(assignments.Where(a => a.AssignmentType == assignmentType).Select(a => a.EmployeeCtrlNbr.Value).ToHashSet());
    }

    private sealed class FakeCraftRoleRepository(IReadOnlyList<CraftRole> roles)
        : FakeRepository<CraftRole>, ICraftRoleRepository
    {
        public override Task<CraftRole?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(roles.FirstOrDefault(r => r.CtrlNbr == ctrlNbr));

        public Task<List<CraftRole>> GetByCraftAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult(roles.Where(r => r.CraftCtrlNbr == craftCtrlNbr).ToList());

        public Task<List<CraftRole>> GetByDepartmentAsync(ControlNumber departmentCtrlNbr)
            => Task.FromResult(new List<CraftRole>());

        public Task<List<CraftRole>> GetByRailroadAsync(ControlNumber railroadCtrlNbr)
            => Task.FromResult(new List<CraftRole>());

        public Task<CraftRole?> GetByCtrlNbrWithQualificationsAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(roles.FirstOrDefault(r => r.CtrlNbr == ctrlNbr));
    }

    private sealed class FakeOnDutyRecordRepository(IReadOnlyList<OnDutyRecord> records)
        : FakeRepository<OnDutyRecord>, IOnDutyRecordRepository
    {
        public Task<IReadOnlyList<OnDutyRecord>> GetRecentForEmployeeAsync(ControlNumber employeeCtrlNbr, int dayCount, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>(records.Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr).ToList());

        public Task<IReadOnlyList<OnDutyRecord>> GetByPositionSlotsAsync(IReadOnlyList<ControlNumber> positionSlotCtrlNbrs, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>(records.Where(r => positionSlotCtrlNbrs.Contains(r.PositionSlotCtrlNbr)).ToList());

        public Task<IReadOnlyList<OnDutyRecord>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>(records.Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr).OrderByDescending(r => r.OnDutyTimeUtc).ToList());

        public Task<IReadOnlyList<OnDutyRecord>> GetOpenForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>(records.Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.Status != OnDutyStatus.TiedUp).ToList());

        public Task<IReadOnlyList<OnDutyRecord>> GetIncompleteForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>(records.Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.CompletionStatus != OnDutyCompletionStatus.Completed).ToList());

        public Task<IReadOnlyList<OnDutyRecord>> GetNotStartedForRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>([]);

        public Task<IReadOnlyList<OnDutyRecord>> GetForEmployeeInRangeAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>(records.Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.OnDutyTimeUtc >= startUtc && r.OnDutyTimeUtc < endUtc).ToList());

        public Task<IReadOnlyList<OnDutyRecord>> GetWorkedForEmployeeInRangeAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>(records.Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.OnDutyTimeUtc >= startUtc && r.OnDutyTimeUtc < endUtc).ToList());

        public Task<IReadOnlyList<OnDutyCompletionStatus>> GetCompletionStatusesForShiftAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyCompletionStatus>>([]);

        public Task<OnDutyTieUpContext?> GetTieUpContextAsync(ControlNumber onDutyRecordCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<OnDutyTieUpContext?>(null);
    }

    private sealed class FakePositionVacancyRepository : FakeRepository<PositionVacancy>, IPositionVacancyRepository
    {
        public List<PositionVacancy> Seeded { get; } = [];

        public override void Add(PositionVacancy entity) => Seeded.Add(entity);

        public override void Update(PositionVacancy entity)
        {
            // entity is tracked by reference in test list; no-op.
        }

        public Task<List<PositionVacancy>> GetOpenAsync() => Task.FromResult(Seeded.Where(v => v.Status is "Open" or "Bulletined").ToList());
        public Task<List<PositionVacancy>> GetOpenByRailroadAsync(ControlNumber railroadCtrlNbr) => Task.FromResult(new List<PositionVacancy>());
        public Task<List<PositionVacancy>> GetByTargetAsync(string targetType, ControlNumber targetCtrlNbr)
            => Task.FromResult(Seeded.Where(v => v.TargetType == targetType && v.TargetCtrlNbr == targetCtrlNbr).ToList());
        public Task<List<PositionVacancy>> GetByCraftAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult(Seeded.Where(v => v.CraftCtrlNbr == craftCtrlNbr).ToList());
        public Task<List<PositionVacancy>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr)
            => Task.FromResult(Seeded.Where(v => v.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr).ToList());
        public Task<double> GetAverageDailyBoardVacanciesAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(0d);
    }

    private sealed class FakeDispatchProjectionRepository : FakeRepository<DispatchProjection>, IDispatchProjectionRepository
    {
        public List<DispatchProjection> Seeded { get; } = [];

        public Task<List<DispatchProjection>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr)
            => Task.FromResult(Seeded.Where(p => p.PositionSlotCtrlNbr == positionSlotCtrlNbr).ToList());

        public override void Add(DispatchProjection entity) => Seeded.Add(entity);

        public override void Remove(DispatchProjection entity)
            => Seeded.Remove(entity);
    }

    private sealed class FakeOrchestrationUow(
        IShiftInstanceRepository shiftInstances,
        IWorkInstanceRepository workInstances,
        ICrewPositionRepository crewPositions,
        IPositionAssignmentRepository positionAssignments,
        ICraftRoleRepository craftRoles,
        IPositionVacancyRepository positionVacancies,
        IDispatchProjectionRepository dispatchProjections,
        IOnDutyRecordRepository? onDutyRecords = null) : IOrchestrationUnitOfWork
    {
        public string CorrelationId => string.Empty;
        public string OrchestrationId => string.Empty;
        public IShiftInstanceRepository ShiftInstances => shiftInstances;
        public IWorkInstanceRepository WorkInstances => workInstances;
        public ICrewPositionRepository CrewPositions => crewPositions;
        public IPositionAssignmentRepository PositionAssignments => positionAssignments;
        public ICraftRoleRepository CraftRoles => craftRoles;
        public IPositionVacancyRepository PositionVacancies => positionVacancies;
        public IDispatchProjectionRepository DispatchProjections => dispatchProjections;

        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }

        public IEmployeeRepository Employees => null!;
        public IEmailAddressRepository EmailAddresses => null!;
        public IParentRepository Parents => null!;
        public IAddressTypeRepository AddressTypes => null!;
        public IPhoneNumberTypeRepository PhoneNumberTypes => null!;
        public IEmailAddressTypeRepository EmailAddressTypes => null!;
        public IEmploymentStatusRepository EmploymentStatuses => null!;
        public IEmploymentStatusHistoryRepository EmploymentStatusHistory => null!;
        public IEmployeePriorServiceCreditRepository EmployeePriorServiceCredits => null!;
        public ICraftRepository Crafts => null!;
        public IRosterRepository Rosters => null!;
        public ISeniorityRepository Seniority => null!;
        public ISeniorityStateRepository SeniorityStates => null!;
        public IGroupTypeRepository GroupTypes => null!;
        public IDynamicGroupRepository DynamicGroups => null!;
        public IGroupAttributeDefinitionRepository AttributeDefinitions => null!;
        public IGroupAttributeValueRepository AttributeValues => null!;
        public IStaffablePositionRepository StaffablePositions => null!;
        public IBoardCascadePolicyRepository BoardCascadePolicies => null!;
        public IRequiredPositionsStrategyRepository RequiredPositionsStrategies => null!;
        public ICraftRequiredPositionsStrategyRepository CraftRequiredPositionsStrategies => null!;
        public IRosterBoardRepository RosterBoards => null!;
        public ICrewRepository Crews => null!;
        public ICrewIncumbencyRepository CrewIncumbencies => null!;
        public ICrewAssignmentRepository CrewAssignments => null!;
        public ICrewAttachmentInstanceRepository CrewAttachmentInstances => null!;
        public IAssignmentRepository Assignments => null!;
        public IAssignmentScheduleRepository AssignmentSchedules => null!;
        public ICraftRoleQualificationRepository CraftRoleQualifications => null!;
        public IPositionSlotRepository PositionSlots => null!;
        public ISlotRequirementRepository SlotRequirements => null!;
        public IShiftDefinitionRepository ShiftDefinitions => null!;
        public IOnDutyRecordRepository OnDutyRecords => onDutyRecords ?? new FakeOnDutyRecordRepository([]);
        public IOffDutyRecordRepository OffDutyRecords => null!;
        public ICraftOperationsPolicyRepository CraftOperationsPolicies => null!;
        public ICraftDisplacementPolicyRepository CraftDisplacementPolicies => null!;
        public IDisplacementCaseRepository DisplacementCases => null!;
        public IDisplacementClaimRepository DisplacementClaims => null!;
        public IBulletinPolicyRepository BulletinPolicies => null!;
        public IAbsenceApprovalPolicyRepository AbsenceApprovalPolicies => null!;
        public ICallSheetRuleRepository CallSheetRules => null!;
        public ICraftCallSheetRuleRepository CraftCallSheetRules => null!;
        public IDepartmentReassignmentRuleRepository DepartmentReassignmentRules => null!;
        public ISeniorityMovePolicyRepository SeniorityMovePolicies => null!;
        public ISeniorityMoveRepository SeniorityMoves => null!;
        public IDispatchDecisionLogRepository DispatchDecisionLogs => null!;
        public IDispatchOverrideRepository DispatchOverrides => null!;
        public IEmployeeBookingRepository EmployeeBookings => null!;
        public IEmployeeCertificationRepository EmployeeCertifications => null!;
        public IEmployeeCertificationReadRepository EmployeeCertificationReads => null!;
        public IFraCertificationConfigRepository FraCertificationConfigs => null!;
        public IFraCertificationCheckConfigRepository FraCertificationCheckConfigs => null!;
        public IFraDutyTourRepository FraDutyTours => null!;
        public IRegulatoryStandardRepository RegulatoryStandards => null!;
        public IRegulatoryQualificationRepository RegulatoryQualifications => null!;
        public ICertificationRevocationRepository CertificationRevocations => null!;
        public IDrugAlcoholTestRepository DrugAlcoholTests => null!;
        public IDrugAlcoholActionRepository DrugAlcoholActions => null!;
        public IVoluntaryReferralRepository VoluntaryReferrals => null!;
        public IQualificationTypeRepository QualificationTypes => null!;
        public IQualificationRequirementRepository QualificationRequirements => null!;
        public IEmployeeQualificationRepository EmployeeQualifications => null!;
        public IEmployeeQualificationSuspensionRepository QualificationSuspensions => null!;
        public IAbsenceRequestRepository AbsenceRequests => null!;
        public IVacancyImpactRepository VacancyImpacts => null!;
        public ISafetyObservationRepository SafetyObservations => null!;
        public ISafetyObservationResolutionRepository SafetyResolutions => null!;
        public ISafetyCategoryRepository SafetyCategories => null!;
        public IRailroadInformationRepository RailroadInformation => null!;
        public IRailroadInformationReadReceiptRepository RailroadInformationReadReceipts => null!;
        public ITimeEntryRepository TimeEntries => null!;
        public IPayrollRunRepository PayrollRuns => null!;
        public IPayrollRecordRepository PayrollRecords => null!;
        public IPayrollExportBatchRepository PayrollExportBatches => null!;
        public IPayrollImportRecordRepository PayrollImportRecords => null!;
        public IHolidayRepository Holidays => null!;
        public IHolidayQualificationRuleRepository HolidayQualificationRules => null!;
        public IHolidayPayrollRecordRepository HolidayPayrollRecords => null!;
        public IEarningCodeRuleRepository EarningCodeRules => null!;
        public IPayRateRepository PayRates => null!;
        public IRailroadHolidaySelectionRepository RailroadHolidaySelections => null!;
        public IRoleRepository Roles => null!;
        public IFeatureRepository Features => null!;
        public IPermissionRepository Permissions => null!;
        public IBulletinRepository Bulletins => null!;
        public IBulletinBidRepository BulletinBids => null!;
        public IBulletinRuleRepository BulletinRules => null!;
        public IEmployeeNotificationRepository EmployeeNotifications => null!;
        public INotificationTypeConfigRepository NotificationTypeConfigs => null!;
        public IUserParentAssignmentRepository UserParentAssignments => null!;
        public IInvitationRepository Invitations => null!;
        public IPayrollTierRepository PayrollTiers => null!;
        public IDepartmentRepository Departments => null!;
        public IPendingSeniorityStateChangeRepository PendingSeniorityStateChanges => null!;
        public IAbsenceRequestWaitListRecordRepository AbsenceRequestWaitListRecords => null!;
        public IAbsenceRequestWaitListLinkRepository AbsenceRequestWaitListLinks => null!;
        public IBoardSnapshotRepository BoardSnapshots => null!;
        public IBoardSelectionDecisionRepository BoardSelectionDecisions => null!;

        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private abstract class FakeRepository<TEntity> : IRepository<TEntity> where TEntity : Entity
    {
        public virtual Task<List<TEntity>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<TEntity>());
        public virtual Task<List<TEntity>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<TEntity>());
        public virtual Task<TEntity?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<TEntity?>(null);
        public virtual Task<TEntity?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<TEntity?>(null);
        public virtual Task AddAsync(TEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task UpdateAsync(TEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual void Add(TEntity entity) { }
        public virtual void Update(TEntity entity) { }
        public virtual void Remove(TEntity entity) { }
    }
}
