using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Bulletins;
using CrewService.Application.DailyOperations;
using CrewService.Application.Authorization;
using CrewService.Application.Notifications;
using CrewService.Application.Policies;
using CrewService.Application.SeniorityOps;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.UnitTests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrewService.UnitTests.BackgroundWorkers;

public sealed class BackgroundJobNextRunResolverTests : IDisposable
{
    private readonly SeniorityVacancyTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task ResolveAsync_Bulletin_ReturnsNull_WhenNextEventBelongsToDifferentWorkArea()
    {
        var seeded = await SeedScenarioAsync();
        var sut = CreateResolver();

        var next = await sut.ResolveAsync(
            workerType: "Bulletin",
            workAreaGroupCtrlNbr: seeded.WorkAreaOneCtrlNbr,
            owningRailroadCtrlNbr: seeded.RailroadCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.Null(next);
    }

    [Fact]
    public async Task ResolveAsync_Bulletin_ReturnsUtc_WhenNextEventMatchesWorkArea()
    {
        var seeded = await SeedScenarioAsync();
        var sut = CreateResolver();

        var next = await sut.ResolveAsync(
            workerType: "Bulletin",
            workAreaGroupCtrlNbr: seeded.WorkAreaTwoCtrlNbr,
            owningRailroadCtrlNbr: seeded.RailroadCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.NotNull(next);
        Assert.Equal(DateTimeKind.Utc, next!.NextUtc.Kind);
        Assert.Equal(
            DateTime.SpecifyKind(seeded.BulletinAssignmentReadyUtc, DateTimeKind.Utc),
            next.NextUtc,
            precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ResolveAsync_SeniorityMove_ReturnsEarliestUtc_ForRailroad()
    {
        var seeded = await SeedScenarioAsync();
        var sut = CreateResolver();

        var next = await sut.ResolveAsync(
            workerType: "SeniorityMove",
            workAreaGroupCtrlNbr: seeded.WorkAreaOneCtrlNbr,
            owningRailroadCtrlNbr: seeded.RailroadCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.NotNull(next);
        Assert.Equal(DateTimeKind.Utc, next!.NextUtc.Kind);
        Assert.Equal(DateTime.SpecifyKind(seeded.SeniorityMoveEventUtc, DateTimeKind.Utc), next.NextUtc);
    }

    [Fact]
    public async Task ResolveAsync_SeniorityStateChange_RespectsWorkAreaFilter()
    {
        var seeded = await SeedScenarioAsync();
        var sut = CreateResolver();

        var nextForOtherWorkArea = await sut.ResolveAsync(
            workerType: "SeniorityStateChange",
            workAreaGroupCtrlNbr: seeded.WorkAreaOneCtrlNbr,
            owningRailroadCtrlNbr: seeded.RailroadCtrlNbr,
            TestContext.Current.CancellationToken);

        var nextForMatchingWorkArea = await sut.ResolveAsync(
            workerType: "SeniorityStateChange",
            workAreaGroupCtrlNbr: seeded.WorkAreaTwoCtrlNbr,
            owningRailroadCtrlNbr: seeded.RailroadCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.Null(nextForOtherWorkArea);
        Assert.NotNull(nextForMatchingWorkArea);
        Assert.Equal(DateTime.SpecifyKind(seeded.StateChangeEventUtc, DateTimeKind.Utc), nextForMatchingWorkArea!.NextUtc);
    }

    [Fact]
    public async Task ResolveAsync_MarkOff_ReturnsEarliestApprovedAutoMarkOffUtc()
    {
        var seeded = await SeedScenarioAsync();
        var sut = CreateResolver();
        var ct = TestContext.Current.CancellationToken;

        var nowUtc = DateTime.UtcNow;
        await using (var ctx = _host.CreateReadContext())
        {
            var req = AbsenceRequest.CreateWithCode(
                seeded.EmployeeCtrlNbr,
                nowUtc.AddMinutes(30),
                null,
                seeded.AbsenceCodeCtrlNbr,
                "MARKOFF",
                autoMarkOffOnApproval: true);
            req.Approve(seeded.EmployeeCtrlNbr);

            ctx.Set<AbsenceRequest>().Add(req);
            await ctx.SaveChangesAsync(ct);
        }

        var next = await sut.ResolveAsync(
            workerType: "MarkOff",
            workAreaGroupCtrlNbr: seeded.WorkAreaOneCtrlNbr,
            owningRailroadCtrlNbr: seeded.RailroadCtrlNbr,
            ct);

        Assert.NotNull(next);
        Assert.Equal(DateTimeKind.Utc, next!.NextUtc.Kind);
    }

    private BackgroundJobNextRunResolver CreateResolver()
    {
        var policiesService = CreatePoliciesService();
        return new BackgroundJobNextRunResolver(
            new NoOpCallSheetSchedulerService(),
            new DailyCallSheetManualOverrideStore(),
            _host.Bulletins,
            policiesService,
            _host.Seniority,
            _host.UowFactory);
    }

    private PoliciesService CreatePoliciesService()
    {
        var notifications = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            new RailroadResolver(),
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));

        var execution = new SeniorityMoveExecutionService(
            _host.UowFactory,
            NullLogger<SeniorityMoveExecutionService>.Instance,
            notifications);

        return new PoliciesService(
            _host.UowFactory,
            new SeniorityMoveSignal(),
            new AbsenceMarkOffSignal(),
            new WorkAreaClock(TimeProvider.System, _host.UowFactory),
            notifications,
            new TestCurrentUserService(),
            execution,
            new TestRequestActorContextResolver(),
            new RequestActorContextPolicy(),
            new RailroadResolver());
    }

    private async Task<SeededScenario> SeedScenarioAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = _host.CreateReadContext();

        var parent = Parent.Create("Resolver Parent");
        ctx.Parents.Add(parent);
        await ctx.SaveChangesAsync(ct);

        var groupType = GroupType.Create("Railroad", null, true);
        ctx.Set<GroupType>().Add(groupType);
        await ctx.SaveChangesAsync(ct);

        var railroad = DynamicGroup.Create(
            groupType.CtrlNbr,
            "Resolver Railroad",
            null,
            null,
            false,
            "RR",
            parentCtrlNbr: parent.CtrlNbr);
        ctx.DynamicGroups.Add(railroad);
        await ctx.SaveChangesAsync(ct);

        var workAreaOne = DynamicGroup.Create(
            groupType.CtrlNbr,
            "Work Area One",
            null,
            null,
            true,
            "WA1",
            parentCtrlNbr: parent.CtrlNbr,
            railroadCtrlNbr: railroad.CtrlNbr,
            timeZoneId: "UTC");

        var workAreaTwo = DynamicGroup.Create(
            groupType.CtrlNbr,
            "Work Area Two",
            null,
            null,
            true,
            "WA2",
            parentCtrlNbr: parent.CtrlNbr,
            railroadCtrlNbr: railroad.CtrlNbr,
            timeZoneId: "UTC");

        ctx.DynamicGroups.AddRange(workAreaOne, workAreaTwo);

        var craft = Craft.Create(null, railroad.CtrlNbr, "Engineer", "Engineers", 1, false, false, 0, 0, 0, 0, 0, false, false, false, 0);
        ctx.Crafts.Add(craft);

        var roster = Roster.Create(craft.CtrlNbr, workAreaTwo.CtrlNbr, null, "Engineer Roster", "Engineer Rosters", 1);
        ctx.Rosters.Add(roster);

        var status = EmploymentStatus.Create(workAreaTwo.CtrlNbr, "ACT", "Active", 1, "A");
        ctx.EmploymentStatuses.Add(status);

        var stateFrom = SeniorityState.Create("Active", StateType.Active, parent.CtrlNbr.Value);
        var stateTo = SeniorityState.Create("Inactive", StateType.Inactive, parent.CtrlNbr.Value);
        ctx.SeniorityStates.AddRange(stateFrom, stateTo);

        await ctx.SaveChangesAsync(ct);

        var employee = Employee.Create(
            workAreaTwo.CtrlNbr,
            "resolver.emp",
            "E900",
            "000-00-0900",
            Gender.Male,
            Race.White,
            new DateTime(1990, 1, 1),
            DateTime.UtcNow,
            status.CtrlNbr,
            "resolver@example.com",
            "admin",
            "Admin User");
        ctx.Employees.Add(employee);
        await ctx.SaveChangesAsync(ct);

        var absenceCode = AbsenceCode.Create(
            railroad.CtrlNbr.Value,
            "MK",
            "Mark Off",
            isExcused: true,
            isCompensated: false,
            requiresApproval: true,
            isSystemOnly: false,
            isHolidayExempt: false,
            defaultAutoMarkUpHours: null,
            isActive: true);
        ctx.Set<AbsenceCode>().Add(absenceCode);
        await ctx.SaveChangesAsync(ct);

        var seniority = Seniority.Create(
            roster.CtrlNbr,
            employee.CtrlNbr,
            lastActiveRoster: true,
            rosterDate: DateTime.UtcNow.Date.AddDays(-30),
            rank: 1,
            seniorityStateCtrlNbr: stateFrom.CtrlNbr,
            canTrain: true);
        ctx.Set<Seniority>().Add(seniority);

        var targetPosition = StaffablePosition.Create(StaffablePositionType.Crew);
        ctx.StaffablePositions.Add(targetPosition);

        var bulletinEventUtc = DateTime.UtcNow.AddHours(2);
        var bulletinEffectiveUtc = DateTime.UtcNow.AddDays(1);
        var vacancy = PositionVacancy.Create(
            workAreaTwo.CtrlNbr,
            StaffablePositionType.Crew,
            targetPosition.CtrlNbr,
            craft.CtrlNbr,
            "VACANCY",
            targetName: "Resolver Position");
        var bulletin = Bulletin.Create(
            vacancy.CtrlNbr,
            craft.CtrlNbr,
            DateTime.UtcNow.AddHours(-2),
            bulletinEventUtc,
            bulletinEffectiveUtc);

        var moveEventUtc = DateTime.UtcNow.AddHours(3);
        var move = SeniorityMove.Create(
            railroad.CtrlNbr,
            employee.CtrlNbr,
            craft.CtrlNbr,
            targetPosition.CtrlNbr,
            displacedEmployeeCtrlNbr: null,
            daysOnCurrentPosition: 0,
            effectiveUtc: moveEventUtc);

        var stateChangeEventUtc = DateTime.UtcNow.AddHours(4);
        var pending = PendingSeniorityStateChange.Schedule(
            seniority.CtrlNbr,
            employee.CtrlNbr,
            stateFrom.CtrlNbr,
            stateTo.CtrlNbr,
            stateChangeEventUtc,
            scheduledByUserId: "dispatcher");

        ctx.Set<PositionVacancy>().Add(vacancy);
        ctx.Set<Bulletin>().Add(bulletin);
        ctx.Set<SeniorityMove>().Add(move);
        ctx.Set<PendingSeniorityStateChange>().Add(pending);

        await ctx.SaveChangesAsync(ct);

        return new SeededScenario(
            railroad.CtrlNbr,
            workAreaOne.CtrlNbr,
            workAreaTwo.CtrlNbr,
            employee.CtrlNbr,
            absenceCode.CtrlNbr,
            DateTime.SpecifyKind(bulletinEffectiveUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(moveEventUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(stateChangeEventUtc, DateTimeKind.Utc));
    }

    private sealed record SeededScenario(
        ControlNumber RailroadCtrlNbr,
        ControlNumber WorkAreaOneCtrlNbr,
        ControlNumber WorkAreaTwoCtrlNbr,
        ControlNumber EmployeeCtrlNbr,
        ControlNumber AbsenceCodeCtrlNbr,
        DateTime BulletinAssignmentReadyUtc,
        DateTime SeniorityMoveEventUtc,
        DateTime StateChangeEventUtc);

    private sealed class NoOpCallSheetSchedulerService : IDailyCallSheetSchedulerService
    {
        public Task<DateTime?> GetNextCallSheetEventUtcAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<DateTime?>(null);

        public Task<DailyCallSheetNextEventCandidate?> GetNextCallSheetEventCandidateAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<DailyCallSheetNextEventCandidate?>(null);

        public Task<IReadOnlyList<DailyCallSheetDueWorkItem>> GetDueWorkItemsAsync(ControlNumber workAreaGroupCtrlNbr, DateTime nowUtc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DailyCallSheetDueWorkItem>>([]);
    }
}
