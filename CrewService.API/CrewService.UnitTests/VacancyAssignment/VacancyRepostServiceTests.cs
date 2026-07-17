using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Bulletins;
using CrewService.Application.Notifications;
using CrewService.Application.Qualifications;
using CrewService.Application.TenantConfig;
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
using CrewService.UnitTests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CrewService.UnitTests.VacancyAssignment;

public class VacancyRepostServiceTests
{
    private const string VacatedTargetCancellationReason = "Cancelled because target position no longer has an incumbent and is being filled through bulletin posting.";

    // shared control numbers
    private static readonly ControlNumber WorkAreaCtrlNbr   = ControlNumber.Create(1);
    private static readonly ControlNumber CraftCtrlNbr      = ControlNumber.Create(2);
    private static readonly ControlNumber CraftRoleCtrlNbr  = ControlNumber.Create(3);
    private static readonly ControlNumber CrewCtrlNbr       = ControlNumber.Create(4);
    private static readonly ControlNumber EmployeeCtrlNbr   = ControlNumber.Create(5);
    private static readonly ControlNumber RosterCtrlNbr     = ControlNumber.Create(21);
    private static readonly ControlNumber CrewStaffPos      = ControlNumber.Create(10);
    private static readonly ControlNumber BoardSlot1        = ControlNumber.Create(30);
    private static readonly ControlNumber BoardSlot2        = ControlNumber.Create(31);
    private static readonly ControlNumber BoardSlot3        = ControlNumber.Create(32);

    // ── helpers ──────────────────────────────────────────────────────────────

    private static BulletinRule MakeRule() =>
        BulletinRule.Create(CraftCtrlNbr, 48, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), 5, new TimeSpan(8, 0, 0), 0);

    private static CraftRole MakeCraftRole() =>
        CraftRole.Create(CraftCtrlNbr, "ENG", "Engineer");

    private static Crew MakeCrew() =>
        Crew.Create("REGULAR", WorkAreaCtrlNbr, "Crew A");

    private static CrewPosition MakeCrewPosition(ControlNumber staffablePositionCtrlNbr) =>
        CrewPosition.Create(CrewCtrlNbr, CraftRoleCtrlNbr, 1, staffablePositionCtrlNbr);

    private static Roster MakeRoster() =>
        Roster.Create(CraftCtrlNbr, WorkAreaCtrlNbr, null, "Engineer Roster", "Engineers", 1);

    private static RosterBoard MakeBoard(int requiredPositions, params ControlNumber[] slotStaffablePositionCtrlNbrs)
    {
        var board = RosterBoard.Create(
            CraftCtrlNbr, RosterCtrlNbr, "Extra Board",
            BoardType.ExtraBoard, RotationType.StandardRotation, isActive: true, requiredPositions: requiredPositions);

        var order = 1;
        var employeeSeed = 1000L;
        foreach (var slot in slotStaffablePositionCtrlNbrs)
            board.AddPosition(ControlNumber.Create(employeeSeed++), order++, slot);

        return board;
    }

    private static PositionAssignment MakeBoardAssignment(ControlNumber staffablePositionCtrlNbr, long employeeCtrlNbr) =>
        PositionAssignment.Create(staffablePositionCtrlNbr, ControlNumber.Create(employeeCtrlNbr), PositionAssignmentType.Board);

    private static SeniorityMove MakePendingMove(ControlNumber employeeCtrlNbr, ControlNumber targetPositionCtrlNbr, string moveType) =>
        SeniorityMove.Create(
            railroadCtrlNbr: ControlNumber.Create(500),
            employeeCtrlNbr: employeeCtrlNbr,
            craftCtrlNbr: CraftCtrlNbr,
            targetPositionCtrlNbr: targetPositionCtrlNbr,
            displacedEmployeeCtrlNbr: null,
            daysOnCurrentPosition: 7,
            moveType: moveType,
            effectiveUtc: DateTime.UtcNow.AddHours(2));

    private static SeniorityMove MakeApprovedMove(ControlNumber employeeCtrlNbr, ControlNumber targetPositionCtrlNbr, string moveType)
    {
        var move = MakePendingMove(employeeCtrlNbr, targetPositionCtrlNbr, moveType);
        move.Approve();
        return move;
    }

    private static VacancyRepostService BuildService(FakeOrchestrationUnitOfWork uow)
    {
        var factory = new FakeUowFactory(uow);
        var railroadResolver = new RailroadResolver();
        var notifications = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            railroadResolver,
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));
        var eligibility = new EmployeeEligibilityService(factory);
        var bulletins = new BulletinsService(
            factory, NullLogger<BulletinsService>.Instance, new FakeBulletinScheduleSignal(), notifications, eligibility);
        return new VacancyRepostService(factory, bulletins, NullLogger<VacancyRepostService>.Instance);
    }

    // ── RepostVacatedPositionAsync: crew path ─────────────────────────────────

    [Fact]
    public async Task RepostVacatedPosition_CrewWithRule_OpensVacancyAndBulletin()
    {
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPositionByStaffPos: MakeCrewPosition(CrewStaffPos));
        var sut = BuildService(uow);

        await sut.RepostVacatedPositionAsync(CrewStaffPos, EmployeeCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.Single(uow.FakeVacancies.AddedEntities);
        Assert.Single(uow.FakeBulletins.AddedEntities);
        Assert.True(uow.Committed);
    }

    [Fact]
    public async Task RepostVacatedPosition_CancelsVoluntaryAndHangoutMoves_WhenTargetIncumbentVacated()
    {
        var voluntaryMove = MakePendingMove(ControlNumber.Create(2001), CrewStaffPos, SeniorityMoveType.Voluntary);
        var hangoutMove = MakeApprovedMove(ControlNumber.Create(2002), CrewStaffPos, SeniorityMoveType.Hangout);
        var differentTargetMove = MakePendingMove(ControlNumber.Create(2003), BoardSlot1, SeniorityMoveType.Voluntary);

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(),
            craftRole: MakeCraftRole(),
            crew: MakeCrew(),
            crewPositionByStaffPos: MakeCrewPosition(CrewStaffPos),
            activeSeniorityMoves: [voluntaryMove, hangoutMove, differentTargetMove]);
        var sut = BuildService(uow);

        await sut.RepostVacatedPositionAsync(CrewStaffPos, EmployeeCtrlNbr, TestContext.Current.CancellationToken);

        Assert.Equal(SeniorityMoveStatus.Cancelled, voluntaryMove.Status);
        Assert.Equal(SeniorityMoveStatus.Cancelled, hangoutMove.Status);
        Assert.Equal(VacatedTargetCancellationReason, voluntaryMove.CancellationReason);
        Assert.Equal(VacatedTargetCancellationReason, hangoutMove.CancellationReason);
        Assert.Equal(SeniorityMoveStatus.Pending, differentTargetMove.Status);
        Assert.Null(differentTargetMove.CancellationReason);
    }

    [Fact]
    public async Task RepostVacatedPosition_DoesNotCancelNoAccessMoves_WhenTargetIncumbentVacated()
    {
        var noAccessMove = MakePendingMove(ControlNumber.Create(2101), CrewStaffPos, SeniorityMoveType.NoAccess);

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(),
            craftRole: MakeCraftRole(),
            crew: MakeCrew(),
            crewPositionByStaffPos: MakeCrewPosition(CrewStaffPos),
            activeSeniorityMoves: [noAccessMove]);
        var sut = BuildService(uow);

        await sut.RepostVacatedPositionAsync(CrewStaffPos, EmployeeCtrlNbr, TestContext.Current.CancellationToken);

        Assert.Equal(SeniorityMoveStatus.Pending, noAccessMove.Status);
        Assert.Null(noAccessMove.CancellationReason);
    }

    [Fact]
    public async Task RepostVacatedPosition_CrewWithRule_VacancyReasonAndTargetTypeAreCrew()
    {
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPositionByStaffPos: MakeCrewPosition(CrewStaffPos));
        var sut = BuildService(uow);

        await sut.RepostVacatedPositionAsync(CrewStaffPos, EmployeeCtrlNbr,
            TestContext.Current.CancellationToken);

        var vacancy = Assert.Single(uow.FakeVacancies.AddedEntities);
        Assert.Equal("INCUMBENT_VACATED", vacancy.VacancyReasonCode);
        Assert.Equal(StaffablePositionType.Crew, vacancy.TargetType);
        Assert.Equal(CrewStaffPos, vacancy.TargetCtrlNbr);
        // The vacate bulletin must carry the craft role, not just the crew name
        // (regression: a vacated crew position showed only "Crew A" instead of "Crew A - Engineer").
        Assert.Equal("Crew A - Engineer", vacancy.TargetName);
    }

    [Fact]
    public async Task RepostVacatedPosition_PositionStillFilled_DoesNotRepost()
    {
        var activeAssignment = PositionAssignment.Create(CrewStaffPos, EmployeeCtrlNbr, PositionAssignmentType.Direct);
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPositionByStaffPos: MakeCrewPosition(CrewStaffPos), activeAssignment: activeAssignment);
        var sut = BuildService(uow);

        await sut.RepostVacatedPositionAsync(CrewStaffPos, EmployeeCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.Empty(uow.FakeVacancies.AddedEntities);
        Assert.Empty(uow.FakeBulletins.AddedEntities);
    }

    [Fact]
    public async Task RepostVacatedPosition_ExistingOpenVacancy_DoesNotRepost()
    {
        var existing = PositionVacancy.Create(
            WorkAreaCtrlNbr, StaffablePositionType.Crew, CrewStaffPos, CraftCtrlNbr, "INCUMBENT_VACATED");
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPositionByStaffPos: MakeCrewPosition(CrewStaffPos), existingVacancies: [existing]);
        var sut = BuildService(uow);

        await sut.RepostVacatedPositionAsync(CrewStaffPos, EmployeeCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.Empty(uow.FakeVacancies.AddedEntities);
        Assert.Empty(uow.FakeBulletins.AddedEntities);
    }

    [Fact]
    public async Task RepostVacatedPosition_NoBulletinRule_DoesNotRepost()
    {
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: null, craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPositionByStaffPos: MakeCrewPosition(CrewStaffPos));
        var sut = BuildService(uow);

        await sut.RepostVacatedPositionAsync(CrewStaffPos, EmployeeCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.Empty(uow.FakeVacancies.AddedEntities);
        Assert.Empty(uow.FakeBulletins.AddedEntities);
    }

    [Fact]
    public async Task RepostVacatedPosition_CrewWithNoBulletin_DoesNotRemoveAnySlot()
    {
        // A crew position that produces no bulletin (no rule) is structural, not surplus board
        // capacity, so the surplus-slot removal path must not fire. Guard: no board resolves for
        // a crew staffable position, so nothing is removed or committed by the removal branch.
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: null, craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPositionByStaffPos: MakeCrewPosition(CrewStaffPos));
        var sut = BuildService(uow);

        await sut.RepostVacatedPositionAsync(CrewStaffPos, EmployeeCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.False(uow.Committed);
    }

    // ── RepostVacatedPositionAsync: board path ────────────────────────────────

    [Fact]
    public async Task RepostVacatedPosition_BoardUnderstaffed_OpensVacancy()
    {
        var board = MakeBoard(requiredPositions: 2, BoardSlot1, BoardSlot2, BoardSlot3);
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), roster: MakeRoster(), board: board,
            boardAssignments: [MakeBoardAssignment(BoardSlot2, 2002)]);
        var sut = BuildService(uow);

        await sut.RepostVacatedPositionAsync(BoardSlot1, ct: TestContext.Current.CancellationToken);

        var vacancy = Assert.Single(uow.FakeVacancies.AddedEntities);
        Assert.Equal("BOARD_UNDERSTAFFED", vacancy.VacancyReasonCode);
        Assert.Equal(StaffablePositionType.Board, vacancy.TargetType);
        Assert.Equal(BoardSlot1, vacancy.TargetCtrlNbr);
    }

    [Fact]
    public async Task RepostVacatedPosition_BoardAdequatelyStaffed_RemovesSurplusSlot()
    {
        // Board still meets RequiredPositions after the vacate, so no bulletin is warranted. The
        // now-empty slot is surplus capacity on a dynamically-sized board and must be removed
        // entirely (decision: "for board slots, if no bulletin, remove the position entirely").
        var board = MakeBoard(requiredPositions: 1, BoardSlot1, BoardSlot2);
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), roster: MakeRoster(), board: board,
            boardAssignments: [MakeBoardAssignment(BoardSlot2, 2002)]);
        var sut = BuildService(uow);

        await sut.RepostVacatedPositionAsync(BoardSlot1, ct: TestContext.Current.CancellationToken);

        Assert.Empty(uow.FakeVacancies.AddedEntities);
        Assert.Empty(uow.FakeBulletins.AddedEntities);
        Assert.DoesNotContain(board.Positions, p => p.StaffablePositionCtrlNbr == BoardSlot1);
        Assert.True(uow.Committed);
    }

    // ── RepostBoardPositionIfUnderstaffedAsync ────────────────────────────────

    [Fact]
    public async Task RepostBoardPositionIfUnderstaffed_Understaffed_OpensVacancy()
    {
        var board = MakeBoard(requiredPositions: 2, BoardSlot2, BoardSlot3);
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), roster: MakeRoster(), board: board,
            boardAssignments: [MakeBoardAssignment(BoardSlot2, 2002)]);
        var sut = BuildService(uow);

        await sut.RepostBoardPositionIfUnderstaffedAsync(
            board.CtrlNbr, BoardSlot1, ct: TestContext.Current.CancellationToken);

        var vacancy = Assert.Single(uow.FakeVacancies.AddedEntities);
        Assert.Equal("BOARD_UNDERSTAFFED", vacancy.VacancyReasonCode);
        Assert.Equal(StaffablePositionType.Board, vacancy.TargetType);
    }

    [Fact]
    public async Task RepostBoardPositionIfUnderstaffed_ExistingVacancy_DoesNotRepost()
    {
        var board = MakeBoard(requiredPositions: 2, BoardSlot2, BoardSlot3);
        var existing = PositionVacancy.Create(
            WorkAreaCtrlNbr, StaffablePositionType.Board, BoardSlot1, CraftCtrlNbr, "BOARD_UNDERSTAFFED");
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), roster: MakeRoster(), board: board,
            existingVacancies: [existing]);
        var sut = BuildService(uow);

        await sut.RepostBoardPositionIfUnderstaffedAsync(
            board.CtrlNbr, BoardSlot1, ct: TestContext.Current.CancellationToken);

        Assert.Empty(uow.FakeVacancies.AddedEntities);
    }

    [Fact]
    public async Task RepostBoardPositionIfUnderstaffed_BoardNotFound_DoesNotRepost()
    {
        // No board configured in the UoW, so the board lookup returns null and the call
        // must short-circuit gracefully without opening a vacancy or throwing.
        var uow = new FakeOrchestrationUnitOfWork(bulletinRule: MakeRule(), roster: MakeRoster());
        var sut = BuildService(uow);

        await sut.RepostBoardPositionIfUnderstaffedAsync(
            ControlNumber.Create(999), BoardSlot1, ct: TestContext.Current.CancellationToken);

        Assert.Empty(uow.FakeVacancies.AddedEntities);
        Assert.Empty(uow.FakeBulletins.AddedEntities);
    }

    // ── ReconcileUnbulletinedVacantPositionsAsync ─────────────────────────────

    [Fact]
    public async Task ReconcileUnbulletinedVacantPositions_CrewVacancy_RepostsAndReturnsCount()
    {
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPositionByStaffPos: MakeCrewPosition(CrewStaffPos),
            vacantCrewStaffPositions: [CrewStaffPos]);
        var sut = BuildService(uow);

        var reposted = await sut.ReconcileUnbulletinedVacantPositionsAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, reposted);
        Assert.Single(uow.FakeVacancies.AddedEntities);
    }

    [Fact]
    public async Task ReconcileUnbulletinedVacantPositions_BoardSlotVacant_Reposts()
    {
        // Board requires 3 but only 2 slots are backed by an active assignment; the unassigned
        // slot must be discovered by the sweep and reposted.
        var board = MakeBoard(requiredPositions: 3, BoardSlot1, BoardSlot2, BoardSlot3);
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), roster: MakeRoster(), board: board,
            boardAssignments:
            [
                MakeBoardAssignment(BoardSlot2, 2002),
                MakeBoardAssignment(BoardSlot3, 2003),
            ]);
        var sut = BuildService(uow);

        var reposted = await sut.ReconcileUnbulletinedVacantPositionsAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, reposted);
        var vacancy = Assert.Single(uow.FakeVacancies.AddedEntities);
        Assert.Equal(BoardSlot1, vacancy.TargetCtrlNbr);
        Assert.Equal("BOARD_UNDERSTAFFED", vacancy.VacancyReasonCode);
    }

    [Fact]
    public async Task ReconcileUnbulletinedVacantPositions_BoardFullyStaffed_ReturnsZero()
    {
        // Every board slot is backed by an active assignment, so the anti-join yields no
        // candidates and nothing is reposted.
        var board = MakeBoard(requiredPositions: 2, BoardSlot1, BoardSlot2);
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), roster: MakeRoster(), board: board,
            boardAssignments:
            [
                MakeBoardAssignment(BoardSlot1, 2001),
                MakeBoardAssignment(BoardSlot2, 2002),
            ]);
        var sut = BuildService(uow);

        var reposted = await sut.ReconcileUnbulletinedVacantPositionsAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(0, reposted);
        Assert.Empty(uow.FakeVacancies.AddedEntities);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Fake infrastructure
    // ═══════════════════════════════════════════════════════════════════════════

    private sealed class FakeUowFactory(FakeOrchestrationUnitOfWork uow) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(OrchestrationUnitOfWorkOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(uow);
    }

    private sealed class FakeBulletinScheduleSignal : IBulletinScheduleSignal
    {
        public void Notify(DateTime eventUtc) { }
        public Task WaitAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private abstract class FakeRepoBase<T> : IRepository<T> where T : Entity
    {
        public List<T> AddedEntities  { get; } = [];
        public List<T> RemovedEntities { get; } = [];
        public virtual Task<List<T>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<T>());
        public virtual Task<List<T>> GetAllAsync(int page, int size, CancellationToken ct = default) => Task.FromResult(new List<T>());
        public virtual Task<T?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<T?>(null);
        public virtual Task<T?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<T?>(null);
        public virtual Task AddAsync(T entity, CancellationToken ct = default) { AddedEntities.Add(entity); return Task.CompletedTask; }
        public virtual Task UpdateAsync(T entity, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual void Add(T entity) { AddedEntities.Add(entity); }
        public virtual void Update(T entity) { }
        public virtual void Remove(T entity) { RemovedEntities.Add(entity); }
    }

    private sealed class FakeCrewRepo(Crew? crew) : FakeRepoBase<Crew>, ICrewRepository
    {
        public override Task<Crew?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(crew);
        public Task<List<Crew>> GetByWorkAreaAsync(ControlNumber w) => Task.FromResult(new List<Crew>());
        public Task<List<Crew>> GetByTypeAsync(string t) => Task.FromResult(new List<Crew>());
        public Task<List<Crew>> GetByRailroadAsync(ControlNumber r) => Task.FromResult(new List<Crew>());
        public Task<bool> ExistsByNameInWorkAreaAsync(ControlNumber w, string n, ControlNumber? ex = null) => Task.FromResult(false);
    }

    private sealed class FakeCrewPositionRepo(CrewPosition? positionByStaffPos, IReadOnlyList<ControlNumber>? vacantStaffPositions)
        : FakeRepoBase<CrewPosition>, ICrewPositionRepository
    {
        public Task<List<CrewPosition>> GetByCrewAsync(ControlNumber c) => Task.FromResult(new List<CrewPosition>());
        public Task<List<CrewPosition>> GetByCrewsAsync(IEnumerable<ControlNumber> cs) => Task.FromResult(new List<CrewPosition>());
        public Task<CrewPosition?> GetByStaffablePositionAsync(ControlNumber s)
            => Task.FromResult(positionByStaffPos is not null && positionByStaffPos.StaffablePositionCtrlNbr == s ? positionByStaffPos : null);
        public Task<List<ControlNumber>> GetVacantStaffablePositionCtrlNbrsAsync(CancellationToken ct = default)
            => Task.FromResult(vacantStaffPositions?.ToList() ?? []);
    }

    private sealed class FakeCraftRoleRepo(CraftRole? role) : FakeRepoBase<CraftRole>, ICraftRoleRepository
    {
        public override Task<CraftRole?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(role);
        public Task<List<CraftRole>> GetByCraftAsync(ControlNumber c) => Task.FromResult(new List<CraftRole>());
        public Task<List<CraftRole>> GetByDepartmentAsync(ControlNumber d) => Task.FromResult(new List<CraftRole>());
        public Task<List<CraftRole>> GetByRailroadAsync(ControlNumber r) => Task.FromResult(new List<CraftRole>());
        public Task<CraftRole?> GetByCtrlNbrWithQualificationsAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<CraftRole?>(null);
    }

    private sealed class FakeBulletinRuleRepo(BulletinRule? rule) : FakeRepoBase<BulletinRule>, IBulletinRuleRepository
    {
        public Task<BulletinRule?> GetByCraftAsync(ControlNumber craftCtrlNbr) => Task.FromResult(rule);
    }

    private sealed class FakeRosterBoardRepo(RosterBoard? board) : FakeRepoBase<RosterBoard>, IRosterBoardRepository
    {
        public override Task<List<RosterBoard>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(board is not null ? new List<RosterBoard> { board } : []);
        public override Task<RosterBoard?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(board is not null && board.CtrlNbr == ctrlNbr ? board : null);
        public Task<IReadOnlyList<RosterBoard>> GetActiveByWorkAreaAsync(ControlNumber w, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RosterBoard>>([]);
        public Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrAsync(ControlNumber c, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RosterBoard>>([]);
        public Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> cs, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RosterBoard>>([]);
        public Task<RosterBoard?> GetByPositionCtrlNbrAsync(ControlNumber p, CancellationToken ct = default) => Task.FromResult<RosterBoard?>(null);
        public Task<RosterBoard?> GetByStaffablePositionCtrlNbrAsync(ControlNumber s, CancellationToken ct = default)
            => Task.FromResult(board is not null && board.Positions.Any(p => p.StaffablePositionCtrlNbr == s) ? board : null);
    }

    private sealed class FakeRosterRepo(Roster? roster) : FakeRepoBase<Roster>, IRosterRepository
    {
        public override Task<Roster?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(roster);
        public Task<List<Roster>> GetByCraftCtrlNbrAsync(ControlNumber c) => Task.FromResult(new List<Roster>());
        public Task<List<Roster>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> cs) => Task.FromResult(new List<Roster>());
        public Task<List<Roster>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> cs, CancellationToken ct = default) => Task.FromResult(new List<Roster>());
        public Task<Roster?> GetTrainingRosterByCraftAsync(ControlNumber c, CancellationToken ct = default) => Task.FromResult<Roster?>(null);
    }

    private sealed class FakeVacancyRepo(IReadOnlyList<PositionVacancy>? existing) : FakeRepoBase<PositionVacancy>, IPositionVacancyRepository
    {
        public Task<List<PositionVacancy>> GetOpenAsync() => Task.FromResult(new List<PositionVacancy>());
        public Task<List<PositionVacancy>> GetOpenByRailroadAsync(ControlNumber r) => Task.FromResult(new List<PositionVacancy>());
        public Task<List<PositionVacancy>> GetByTargetAsync(string t, ControlNumber c)
            => Task.FromResult((existing ?? []).Where(v => v.TargetType == t && v.TargetCtrlNbr == c).ToList());
        public Task<List<PositionVacancy>> GetByCraftAsync(ControlNumber c) => Task.FromResult(new List<PositionVacancy>());
        public Task<List<PositionVacancy>> GetByWorkAreaAsync(ControlNumber w) => Task.FromResult(new List<PositionVacancy>());
        public Task<double> GetAverageDailyBoardVacanciesAsync(ControlNumber w, ControlNumber c, CancellationToken ct = default) => Task.FromResult(0.0);
    }

    private sealed class FakeBulletinRepo : FakeRepoBase<Bulletin>, IBulletinRepository
    {
        public Task<Bulletin?> GetByVacancyAsync(ControlNumber v) => Task.FromResult<Bulletin?>(null);
        public Task<List<Bulletin>> GetPostedAsync() => Task.FromResult(new List<Bulletin>());
        public Task<List<Bulletin>> GetPostedByRailroadAsync(ControlNumber r) => Task.FromResult(new List<Bulletin>());
        public Task<List<Bulletin>> GetPostedByCraftAsync(ControlNumber c) => Task.FromResult(new List<Bulletin>());
        public Task<List<Bulletin>> GetActiveAsync() => Task.FromResult(new List<Bulletin>());
        public Task<List<Bulletin>> GetActiveByRailroadAsync(ControlNumber r) => Task.FromResult(new List<Bulletin>());
        public Task<List<Bulletin>> GetByStatusAsync(string s) => Task.FromResult(new List<Bulletin>());
        public Task<List<Bulletin>> GetByWorkAreaAsync(ControlNumber w) => Task.FromResult(new List<Bulletin>());
        public Task<List<Bulletin>> GetNoBidPastDeadlineAsync(CancellationToken ct = default) => Task.FromResult(new List<Bulletin>());
        public Task<List<Bulletin>> GetInDateRangeAsync(DateTime fromUtc, ControlNumber? railroadCtrlNbr = null) => Task.FromResult(new List<Bulletin>());
        public Task<Bulletin?> GetNextPendingEventBulletinAsync(CancellationToken ct = default) => Task.FromResult<Bulletin?>(null);
        public Task<List<Bulletin>> GetClosedUnawardedAsync(CancellationToken ct = default) => Task.FromResult(new List<Bulletin>());
        public Task<DateTime?> GetNextPendingEventUtcAsync(CancellationToken ct = default) => Task.FromResult<DateTime?>(null);
    }

    private sealed class FakePositionAssignmentRepo(IReadOnlyList<PositionAssignment> assignments) : FakeRepoBase<PositionAssignment>, IPositionAssignmentRepository
    {
        public Task<PositionAssignment?> GetByStaffablePositionAsync(ControlNumber s)
            => Task.FromResult(assignments.FirstOrDefault(a => a.StaffablePositionCtrlNbr == s));
        public Task<List<PositionAssignment>> GetByStaffablePositionsAsync(IEnumerable<ControlNumber> ctrlNbrs)
        {
            var set = ctrlNbrs.ToHashSet();
            return Task.FromResult(assignments.Where(a => set.Contains(a.StaffablePositionCtrlNbr)).ToList());
        }
        public Task<List<PositionAssignment>> GetByEmployeeAsync(ControlNumber e) => Task.FromResult(new List<PositionAssignment>());
        public Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsAsync() => Task.FromResult(new HashSet<long>());
        public Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsByTypeAsync(string t) => Task.FromResult(new HashSet<long>());
    }

    private sealed class FakeSeniorityMoveRepo(IReadOnlyList<SeniorityMove> activeMoves) : FakeRepoBase<SeniorityMove>, ISeniorityMoveRepository
    {
        public Task<List<SeniorityMove>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(activeMoves.Where(m => m.EmployeeCtrlNbr == employeeCtrlNbr).ToList());
        public Task<List<SeniorityMove>> GetByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(activeMoves.Where(m => m.CraftCtrlNbr == craftCtrlNbr).ToList());
        public Task<List<SeniorityMove>> GetByStatusAsync(string status, CancellationToken ct = default)
            => Task.FromResult(activeMoves.Where(m => m.Status == status).ToList());
        public Task<List<SeniorityMove>> GetByCraftByStatusAsync(ControlNumber craftCtrlNbr, string status, CancellationToken ct = default)
            => Task.FromResult(activeMoves.Where(m => m.CraftCtrlNbr == craftCtrlNbr && m.Status == status).ToList());
        public Task<List<SeniorityMove>> GetPendingAsync(CancellationToken ct = default)
            => Task.FromResult(activeMoves.Where(m => m.Status == SeniorityMoveStatus.Pending).ToList());
        public Task<List<SeniorityMove>> GetActiveAsync(CancellationToken ct = default)
            => Task.FromResult(activeMoves.Where(m => m.Status == SeniorityMoveStatus.Pending || m.Status == SeniorityMoveStatus.Approved).ToList());
        public Task<List<SeniorityMove>> GetAllMovesAsync(CancellationToken ct = default) => Task.FromResult(activeMoves.ToList());
        public Task<List<SeniorityMove>> GetApprovedDueAsync(DateTime asOf, CancellationToken ct = default)
            => Task.FromResult(activeMoves.Where(m => m.Status == SeniorityMoveStatus.Approved && m.EffectiveUtc <= asOf).ToList());
        public Task<DateTime?> GetNextApprovedEffectiveUtcAsync(CancellationToken ct = default)
            => Task.FromResult(activeMoves
                .Where(m => m.Status == SeniorityMoveStatus.Approved && m.EffectiveUtc.HasValue)
                .OrderBy(m => m.EffectiveUtc)
                .Select(m => m.EffectiveUtc)
                .FirstOrDefault());
        public Task<List<SeniorityMove>> GetPendingByTargetPositionAsync(ControlNumber targetPositionCtrlNbr, ControlNumber excludeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(activeMoves
                .Where(m => m.Status == SeniorityMoveStatus.Pending && m.TargetPositionCtrlNbr == targetPositionCtrlNbr && m.CtrlNbr != excludeCtrlNbr)
                .ToList());
    }

    private sealed class FakeStaffablePositionRepo : FakeRepoBase<StaffablePosition>, IStaffablePositionRepository
    {
        public Task<List<StaffablePosition>> GetByPositionTypeAsync(string t) => Task.FromResult(new List<StaffablePosition>());
    }

    private sealed class FakeDynamicGroupRepo(DynamicGroup? workArea) : FakeRepoBase<DynamicGroup>, IDynamicGroupRepository
    {
        public override Task<DynamicGroup?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult(workArea);
        public Task<List<DynamicGroup>> GetByParentCtrlNbrAsync(ControlNumber? p) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> cs) => Task.FromResult(new List<DynamicGroup>());
        public Task<DynamicGroup?> GetByGroupTypeAndNameIncludingDeletedAsync(ControlNumber g, string n) => Task.FromResult<DynamicGroup?>(null);
        public Task<List<DynamicGroup>> GetWorkAreasAsync(ControlNumber? r = null) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetWorkAreasWithDescendantsAsync() => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetAncestorsAsync(ControlNumber g) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetTreeAsync(ControlNumber? r = null) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetByGroupTypeNameAsync(string t, ControlNumber? p = null) => Task.FromResult(new List<DynamicGroup>());
        public Task BackfillPathsAsync() => Task.CompletedTask;
    }

    private sealed class FakeEmployeeNotificationRepo : FakeRepoBase<EmployeeNotification>, IEmployeeNotificationRepository
    {
        public Task<List<EmployeeNotification>> GetByEmployeeAsync(ControlNumber e, CancellationToken ct = default) => Task.FromResult(new List<EmployeeNotification>());
        public Task<List<EmployeeNotification>> GetUnacknowledgedByEmployeeAsync(ControlNumber e, CancellationToken ct = default) => Task.FromResult(new List<EmployeeNotification>());
        public Task<List<EmployeeNotification>> GetByRailroadAsync(ControlNumber r, CancellationToken ct = default) => Task.FromResult(new List<EmployeeNotification>());
        public Task<int> CountUnacknowledgedByRailroadAsync(ControlNumber r, CancellationToken ct = default) => Task.FromResult(0);
    }

    private sealed class FakeOrchestrationUnitOfWork : IOrchestrationUnitOfWork
    {
        public bool Committed { get; private set; }

        public FakeVacancyRepo            FakeVacancies          { get; }
        public FakeBulletinRepo           FakeBulletins          { get; } = new();
        public FakePositionAssignmentRepo FakePositionAssignments { get; }
        private readonly FakeCrewRepo             _crews;
        private readonly FakeCrewPositionRepo     _crewPositions;
        private readonly FakeCraftRoleRepo        _craftRoles;
        private readonly FakeBulletinRuleRepo     _bulletinRules;
        private readonly FakeRosterBoardRepo      _rosterBoards;
        private readonly FakeRosterRepo           _rosters;
        private readonly FakeStaffablePositionRepo _staffablePositions = new();
        private readonly FakeDynamicGroupRepo     _dynamicGroups;
        private readonly FakeEmployeeNotificationRepo _employeeNotifications = new();
        private readonly FakeSeniorityMoveRepo    _seniorityMoves;

        public FakeOrchestrationUnitOfWork(
            BulletinRule?                    bulletinRule           = null,
            CraftRole?                       craftRole              = null,
            Crew?                            crew                   = null,
            CrewPosition?                    crewPositionByStaffPos = null,
            PositionAssignment?              activeAssignment       = null,
            RosterBoard?                     board                  = null,
            Roster?                          roster                 = null,
            IReadOnlyList<PositionAssignment>? boardAssignments     = null,
            IReadOnlyList<PositionVacancy>?  existingVacancies      = null,
            IReadOnlyList<ControlNumber>?    vacantCrewStaffPositions = null,
            IReadOnlyList<SeniorityMove>?    activeSeniorityMoves   = null)
        {
            _crews          = new FakeCrewRepo(crew);
            _crewPositions  = new FakeCrewPositionRepo(crewPositionByStaffPos, vacantCrewStaffPositions);
            _craftRoles     = new FakeCraftRoleRepo(craftRole);
            _bulletinRules  = new FakeBulletinRuleRepo(bulletinRule);
            _rosterBoards   = new FakeRosterBoardRepo(board);
            _rosters        = new FakeRosterRepo(roster);
            _dynamicGroups  = new FakeDynamicGroupRepo(null);
            _seniorityMoves = new FakeSeniorityMoveRepo(activeSeniorityMoves ?? []);
            FakeVacancies   = new FakeVacancyRepo(existingVacancies);

            var assignments = new List<PositionAssignment>();
            if (activeAssignment is not null) assignments.Add(activeAssignment);
            if (boardAssignments is not null) assignments.AddRange(boardAssignments);
            FakePositionAssignments = new FakePositionAssignmentRepo(assignments);
        }

        public string CorrelationId  => "test";
        public string OrchestrationId => "test";

        public ICrewRepository             Crews              => _crews;
        public ICrewPositionRepository     CrewPositions      => _crewPositions;
        public ICraftRoleRepository        CraftRoles         => _craftRoles;
        public IBulletinRuleRepository     BulletinRules      => _bulletinRules;
        public IRosterBoardRepository      RosterBoards       => _rosterBoards;
        public IRosterRepository           Rosters            => _rosters;
        public IPositionVacancyRepository  PositionVacancies  => FakeVacancies;
        public IBulletinRepository         Bulletins          => FakeBulletins;
        public IPositionAssignmentRepository PositionAssignments => FakePositionAssignments;
        public IStaffablePositionRepository StaffablePositions  => _staffablePositions;
        public IDynamicGroupRepository      DynamicGroups       => _dynamicGroups;
        public IEmployeeNotificationRepository EmployeeNotifications => _employeeNotifications;
        public INotificationTypeConfigRepository NotificationTypeConfigs => null!;

        public Task CommitAsync(CancellationToken ct = default) { Committed = true; return Task.CompletedTask; }
        public Task SaveAsync(CancellationToken ct = default)   => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }

        // ── unused interface members ──────────────────────────────────────────
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken ct = default) => Task.CompletedTask;
        public ICrewIncumbencyRepository                 CrewIncumbencies             => null!;
        public ISeniorityRepository                      Seniority                    => null!;
        public IEmployeeCertificationRepository          EmployeeCertifications       => null!;
        public IBoardCascadePolicyRepository             BoardCascadePolicies         => throw new NotImplementedException();
        public IRequiredPositionsStrategyRepository      RequiredPositionsStrategies  => throw new NotImplementedException();
        public ICraftRequiredPositionsStrategyRepository CraftRequiredPositionsStrategies => throw new NotImplementedException();
        public IAbsenceRequestRepository                 AbsenceRequests              => throw new NotImplementedException();
        public IVacancyImpactRepository                  VacancyImpacts               => throw new NotImplementedException();
        public ISafetyObservationRepository              SafetyObservations           => throw new NotImplementedException();
        public ISafetyObservationResolutionRepository    SafetyResolutions            => throw new NotImplementedException();
        public ISafetyCategoryRepository                 SafetyCategories             => throw new NotImplementedException();
        public IRailroadInformationRepository            RailroadInformation          => throw new NotImplementedException();
        public IRailroadInformationReadReceiptRepository RailroadInformationReadReceipts => throw new NotImplementedException();
        public IShiftInstanceRepository                  ShiftInstances               => throw new NotImplementedException();
        public IOnDutyRecordRepository                   OnDutyRecords                => throw new NotImplementedException();
        public IOffDutyRecordRepository                  OffDutyRecords               => throw new NotImplementedException();
        public ICraftOperationsPolicyRepository          CraftOperationsPolicies      => throw new NotImplementedException();
        public ICraftDisplacementPolicyRepository        CraftDisplacementPolicies    => throw new NotImplementedException();
        public IDisplacementCaseRepository               DisplacementCases            => throw new NotImplementedException();
        public IDisplacementClaimRepository              DisplacementClaims           => throw new NotImplementedException();
        public IBulletinPolicyRepository                 BulletinPolicies             => throw new NotImplementedException();
        public ICallSheetRuleRepository                  CallSheetRules              => throw new NotImplementedException();
        public ICraftCallSheetRuleRepository CraftCallSheetRules => null!;
        public IDepartmentReassignmentRuleRepository     DepartmentReassignmentRules => throw new NotImplementedException();
        public ISeniorityMovePolicyRepository            SeniorityMovePolicies        => throw new NotImplementedException();
        public ISeniorityMoveRepository                  SeniorityMoves               => _seniorityMoves;
        public IRoleRepository                           Roles                        => throw new NotImplementedException();
        public IFeatureRepository                        Features                     => throw new NotImplementedException();
        public IPermissionRepository                     Permissions                  => throw new NotImplementedException();
        public IBulletinBidRepository                    BulletinBids                 => throw new NotImplementedException();
        public IDispatchProjectionRepository             DispatchProjections          => throw new NotImplementedException();
        public IDispatchDecisionLogRepository            DispatchDecisionLogs         => throw new NotImplementedException();
        public IDispatchOverrideRepository               DispatchOverrides            => throw new NotImplementedException();
        public IEmployeeBookingRepository                EmployeeBookings             => throw new NotImplementedException();
        public ITimeEntryRepository                      TimeEntries                  => throw new NotImplementedException();
        public IPayrollRunRepository                     PayrollRuns                  => throw new NotImplementedException();
        public IPayrollRecordRepository                  PayrollRecords               => throw new NotImplementedException();
        public IPayrollExportBatchRepository             PayrollExportBatches         => throw new NotImplementedException();
        public IPayrollImportRecordRepository            PayrollImportRecords         => throw new NotImplementedException();
        public IHolidayRepository                        Holidays                     => throw new NotImplementedException();
        public IHolidayQualificationRuleRepository       HolidayQualificationRules    => throw new NotImplementedException();
        public IHolidayPayrollRecordRepository           HolidayPayrollRecords        => throw new NotImplementedException();
        public IEarningCodeRuleRepository                EarningCodeRules             => throw new NotImplementedException();
        public IPayRateRepository                        PayRates                     => throw new NotImplementedException();
        public IRailroadHolidaySelectionRepository       RailroadHolidaySelections    => throw new NotImplementedException();
        public IEmployeeRepository                       Employees                    => null!;
        public IEmailAddressRepository                   EmailAddresses               => null!;
        public IParentRepository                         Parents                      => null!;
        public IAddressTypeRepository                    AddressTypes                 => null!;
        public IPhoneNumberTypeRepository                PhoneNumberTypes             => null!;
        public IEmailAddressTypeRepository               EmailAddressTypes            => null!;
        public IEmploymentStatusRepository               EmploymentStatuses           => null!;
        public IEmploymentStatusHistoryRepository        EmploymentStatusHistory      => null!;
        public IEmployeePriorServiceCreditRepository     EmployeePriorServiceCredits  => null!;
        public ICraftRepository                          Crafts                       => null!;
        public ISeniorityStateRepository                 SeniorityStates              => null!;
        public IGroupTypeRepository                      GroupTypes                   => null!;
        public IGroupAttributeDefinitionRepository       AttributeDefinitions         => null!;
        public IGroupAttributeValueRepository            AttributeValues              => null!;
        public ICrewAssignmentRepository                 CrewAssignments              => null!;
        public ICrewAttachmentInstanceRepository         CrewAttachmentInstances      => null!;
        public IAssignmentRepository                     Assignments                  => null!;
        public IAssignmentScheduleRepository             AssignmentSchedules          => null!;
        public IDepartmentRepository                     Departments                  => null!;
        public ICraftRoleQualificationRepository         CraftRoleQualifications      => null!;
        public IWorkInstanceRepository                   WorkInstances                => null!;
        public IPositionSlotRepository                   PositionSlots                => null!;
        public ISlotRequirementRepository                SlotRequirements             => null!;
        public IShiftDefinitionRepository                ShiftDefinitions             => null!;
        public IQualificationTypeRepository              QualificationTypes           => null!;
        public IQualificationRequirementRepository       QualificationRequirements    => null!;
        public IEmployeeQualificationRepository          EmployeeQualifications       => null!;
        public IEmployeeQualificationSuspensionRepository QualificationSuspensions    => null!;
        public ICertificationRevocationRepository        CertificationRevocations     => null!;
        public IDrugAlcoholActionRepository              DrugAlcoholActions           => null!;
        public IDrugAlcoholTestRepository                DrugAlcoholTests             => null!;
        public IEmployeeCertificationReadRepository      EmployeeCertificationReads   => null!;
        public IFraCertificationCheckConfigRepository    FraCertificationCheckConfigs => null!;
        public IFraCertificationConfigRepository         FraCertificationConfigs      => null!;
        public IFraDutyTourRepository                    FraDutyTours                 => null!;
        public IRegulatoryQualificationRepository        RegulatoryQualifications     => null!;
        public IRegulatoryStandardRepository             RegulatoryStandards          => null!;
        public IVoluntaryReferralRepository              VoluntaryReferrals           => null!;
        public IUserParentAssignmentRepository           UserParentAssignments        => null!;
        public IInvitationRepository                     Invitations                  => null!;
        public IPayrollTierRepository                    PayrollTiers                 => null!;
        public ISeniorityStateVacancyConfigRepository    SeniorityStateVacancyConfigs => throw new NotImplementedException();
        public IPendingSeniorityStateChangeRepository    PendingSeniorityStateChanges => throw new NotImplementedException();
    }
}
