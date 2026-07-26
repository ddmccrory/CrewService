using CrewService.Application.Crews;
using CrewService.Application.Notifications;
using CrewService.Application.Policies;
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

namespace CrewService.UnitTests.Bulletins;

public class BulletinCreationServiceTests
{
    // shared control numbers
    private static readonly ControlNumber WorkAreaCtrlNbr   = ControlNumber.Create(1);
    private static readonly ControlNumber CraftCtrlNbr      = ControlNumber.Create(2);
    private static readonly ControlNumber CraftRoleCtrlNbr  = ControlNumber.Create(3);
    private static readonly ControlNumber CrewCtrlNbr       = ControlNumber.Create(4);
    private static readonly ControlNumber EmployeeCtrlNbr   = ControlNumber.Create(5);
    private static readonly ControlNumber DepartmentCtrlNbr = ControlNumber.Create(6);

    // ── helpers ──────────────────────────────────────────────────────────────

    private static BulletinRule MakeRule() =>
        BulletinRule.Create(CraftCtrlNbr, 48, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), 5, new TimeSpan(8, 0, 0), 0);

    private static CraftRole MakeCraftRole() =>
        CraftRole.Create(CraftCtrlNbr, "ENG", "Engineer");

    private static Crew MakeCrew() =>
        Crew.Create("REGULAR", WorkAreaCtrlNbr, "Crew A", departmentCtrlNbr: DepartmentCtrlNbr);

    private static Craft MakeCraft() =>
        Craft.Create(
            parentCtrlNbr: null,
            dynamicGroupCtrlNbr: null,
            craftName: "Engineer",
            craftPluralName: "Engineers",
            craftNumber: 1,
            autoMarkUp: false,
            approveAllMarkOffs: false,
            markOffHours: 0,
            markUpHours: 0,
            requiredRestHours: 0,
            maximumVacationDayTime: 0,
            unpaidMealPeriodMinutes: 0,
            hoursofService: false,
            processPayroll: false,
            showNotifications: false,
            vacationAssignmentType: 0,
            departmentCtrlNbr: DepartmentCtrlNbr);

    private static DepartmentReassignmentRule MakeDepartmentRule() =>
        DepartmentReassignmentRule.Create(DepartmentCtrlNbr, BoardType.Hangout, isRequired: true);

    private static CrewPosition MakeCrewPosition(ControlNumber staffablePositionCtrlNbr) =>
        CrewPosition.Create(CrewCtrlNbr, CraftRoleCtrlNbr, 1, staffablePositionCtrlNbr);

    private static CrewsAppService BuildService(FakeOrchestrationUnitOfWork uow) =>
        BuildService(uow, new RecordingVacancyRepostService());

    private static CrewsAppService BuildService(FakeOrchestrationUnitOfWork uow, IVacancyRepostService repost) =>
        new(new FakeUowFactory(uow), repost, new DepartmentReassignmentService(), NullLogger<CrewsAppService>.Instance);

    // ── CreateCrewPositionAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateCrewPosition_WithRule_CreatesBulletinAndVacancy()
    {
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(),
            craft: MakeCraft(), departmentReassignmentRule: MakeDepartmentRule());
        var sut = BuildService(uow);

        await sut.CreateCrewPositionAsync(CrewCtrlNbr.Value, CraftRoleCtrlNbr.Value, 1,
            TestContext.Current.CancellationToken);

        Assert.Single(uow.FakeVacancies.AddedEntities);
        Assert.Single(uow.FakeBulletins.AddedEntities);
        Assert.True(uow.Committed);
    }

    [Fact]
    public async Task CreateCrewPosition_WithRule_VacancyReasonIsPositionCreated()
    {
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(),
            craft: MakeCraft(), departmentReassignmentRule: MakeDepartmentRule());
        var sut = BuildService(uow);

        await sut.CreateCrewPositionAsync(CrewCtrlNbr.Value, CraftRoleCtrlNbr.Value, 1,
            TestContext.Current.CancellationToken);

        Assert.Equal("POSITION_CREATED", uow.FakeVacancies.AddedEntities[0].VacancyReasonCode);
    }

    [Fact]
    public async Task CreateCrewPosition_WithoutRule_NoBulletinCreated()
    {
        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: null, craftRole: MakeCraftRole(), crew: MakeCrew(),
            craft: MakeCraft(), departmentReassignmentRule: MakeDepartmentRule());
        var sut = BuildService(uow);

        await sut.CreateCrewPositionAsync(CrewCtrlNbr.Value, CraftRoleCtrlNbr.Value, 1,
            TestContext.Current.CancellationToken);

        Assert.Empty(uow.FakeVacancies.AddedEntities);
        Assert.Empty(uow.FakeBulletins.AddedEntities);
        Assert.True(uow.Committed);
    }

    // ── EndCrewIncumbencyAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EndCrewIncumbency_VacatesAssignmentAndDelegatesRepost_NoInlineBulletin()
    {
        var staffPos    = StaffablePosition.Create(StaffablePositionType.Crew);
        var crewPos     = MakeCrewPosition(staffPos.CtrlNbr);
        var incumbency  = CrewIncumbency.Create(crewPos.CtrlNbr, EmployeeCtrlNbr, DateTime.UtcNow.AddDays(-1));
        var assignment  = PositionAssignment.Create(staffPos.CtrlNbr, EmployeeCtrlNbr, PositionAssignmentType.Direct, crewPos.CtrlNbr);

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(), craft: MakeCraft(), departmentReassignmentRule: MakeDepartmentRule(),
            crewPosition: crewPos, incumbency: incumbency, positionAssignment: assignment);

        var repost = new RecordingVacancyRepostService();
        var sut = BuildService(uow, repost);

        await sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow,
            TestContext.Current.CancellationToken);

        // The vacate itself does not bulletin inline; it commits the freed position and then
        // delegates to the single canonical VacancyRepostService — the same path board removals use.
        Assert.Contains(assignment, uow.FakePositionAssignments.RemovedEntities);
        Assert.Empty(uow.FakeVacancies.AddedEntities);
        Assert.Empty(uow.FakeBulletins.AddedEntities);
        Assert.True(uow.Committed);
        Assert.Single(repost.RepostedPositions);
        Assert.Equal(staffPos.CtrlNbr, repost.RepostedPositions[0]);
    }

    [Fact]
    public async Task EndCrewIncumbency_DoesNotRaisePositionAssignmentVacatedEvent()
    {
        var staffPos   = StaffablePosition.Create(StaffablePositionType.Crew);
        var crewPos    = MakeCrewPosition(staffPos.CtrlNbr);
        var incumbency = CrewIncumbency.Create(crewPos.CtrlNbr, EmployeeCtrlNbr, DateTime.UtcNow.AddDays(-1));
        var assignment = PositionAssignment.Create(staffPos.CtrlNbr, EmployeeCtrlNbr, PositionAssignmentType.Direct, crewPos.CtrlNbr);

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(), craft: MakeCraft(), departmentReassignmentRule: MakeDepartmentRule(),
            crewPosition: crewPos, incumbency: incumbency, positionAssignment: assignment);

        var sut = BuildService(uow);

        await sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow,
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain(assignment.DomainEvents, e => e is PositionAssignmentVacatedDomainEvent);
    }

    [Fact]
    public async Task EndCrewIncumbency_WithoutPositionAssignment_JustCommits()
    {
        var staffPos   = StaffablePosition.Create(StaffablePositionType.Crew);
        var crewPos    = MakeCrewPosition(staffPos.CtrlNbr);
        var incumbency = CrewIncumbency.Create(crewPos.CtrlNbr, EmployeeCtrlNbr, DateTime.UtcNow.AddDays(-1));

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(), craft: MakeCraft(), departmentReassignmentRule: MakeDepartmentRule(),
            crewPosition: crewPos, incumbency: incumbency);

        var sut = BuildService(uow);

        await sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow,
            TestContext.Current.CancellationToken);

        Assert.Empty(uow.FakePositionAssignments.RemovedEntities);
        Assert.Empty(uow.FakeVacancies.AddedEntities);
        Assert.Empty(uow.FakeBulletins.AddedEntities);
        Assert.True(uow.Committed);
    }

    [Fact]
    public async Task EndCrewIncumbency_RemovesPositionAssignment()
    {
        var staffPos   = StaffablePosition.Create(StaffablePositionType.Crew);
        var crewPos    = MakeCrewPosition(staffPos.CtrlNbr);
        var incumbency = CrewIncumbency.Create(crewPos.CtrlNbr, EmployeeCtrlNbr, DateTime.UtcNow.AddDays(-1));
        var assignment = PositionAssignment.Create(staffPos.CtrlNbr, EmployeeCtrlNbr, PositionAssignmentType.Direct, crewPos.CtrlNbr);

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(), craft: MakeCraft(), departmentReassignmentRule: MakeDepartmentRule(),
            crewPosition: crewPos, incumbency: incumbency, positionAssignment: assignment);

        var sut = BuildService(uow);

        await sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow,
            TestContext.Current.CancellationToken);

        Assert.Contains(assignment, uow.FakePositionAssignments.RemovedEntities);
    }

    [Fact]
    public async Task EndCrewIncumbency_WithDepartmentRule_ReassignsToHangoutBoard()
    {
        var staffPos = StaffablePosition.Create(StaffablePositionType.Crew);
        var crewPos = MakeCrewPosition(staffPos.CtrlNbr);
        var incumbency = CrewIncumbency.Create(crewPos.CtrlNbr, EmployeeCtrlNbr, DateTime.UtcNow.AddDays(-1));
        var assignment = PositionAssignment.Create(staffPos.CtrlNbr, EmployeeCtrlNbr, PositionAssignmentType.Direct, crewPos.CtrlNbr);

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(), craft: MakeCraft(), departmentReassignmentRule: MakeDepartmentRule(),
            crewPosition: crewPos, incumbency: incumbency, positionAssignment: assignment);

        var sut = BuildService(uow);

        await sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow, TestContext.Current.CancellationToken);

        Assert.Contains(uow.FakePositionAssignments.AddedEntities, a =>
            a.EmployeeCtrlNbr == EmployeeCtrlNbr && a.AssignmentType == PositionAssignmentType.Board);
    }

    [Fact]
    public async Task EndCrewIncumbency_WithDepartmentRule_CreatesBoardPlacementNotification()
    {
        var staffPos = StaffablePosition.Create(StaffablePositionType.Crew);
        var crewPos = MakeCrewPosition(staffPos.CtrlNbr);
        var incumbency = CrewIncumbency.Create(crewPos.CtrlNbr, EmployeeCtrlNbr, DateTime.UtcNow.AddDays(-1));
        var assignment = PositionAssignment.Create(staffPos.CtrlNbr, EmployeeCtrlNbr, PositionAssignmentType.Direct, crewPos.CtrlNbr);

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(), craft: MakeCraft(), departmentReassignmentRule: MakeDepartmentRule(),
            crewPosition: crewPos, incumbency: incumbency, positionAssignment: assignment);

        var repost = new RecordingVacancyRepostService();
        var notifications = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            new FixedRailroadResolver(ControlNumber.Create(999)),
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));
        var sut = new CrewsAppService(
            new FakeUowFactory(uow),
            repost,
            new DepartmentReassignmentService(notifications),
            NullLogger<CrewsAppService>.Instance);

        await sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow, TestContext.Current.CancellationToken);

        var notification = Assert.Single(uow.FakeEmployeeNotifications.AddedEntities);
        Assert.Equal(EmployeeCtrlNbr, notification.EmployeeCtrlNbr);
        Assert.Equal(NotificationCategories.BoardPlacement, notification.Category);
    }

    [Fact]
    public async Task EndCrewIncumbency_WithoutDepartmentRule_Throws()
    {
        var staffPos = StaffablePosition.Create(StaffablePositionType.Crew);
        var crewPos = MakeCrewPosition(staffPos.CtrlNbr);
        var incumbency = CrewIncumbency.Create(crewPos.CtrlNbr, EmployeeCtrlNbr, DateTime.UtcNow.AddDays(-1));
        var assignment = PositionAssignment.Create(staffPos.CtrlNbr, EmployeeCtrlNbr, PositionAssignmentType.Direct, crewPos.CtrlNbr);

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(), craft: MakeCraft(), departmentReassignmentRule: null,
            crewPosition: crewPos, incumbency: incumbency, positionAssignment: assignment);

        var sut = BuildService(uow);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow, TestContext.Current.CancellationToken));
    }

    private sealed class FixedRailroadResolver(ControlNumber railroadCtrlNbr) : IRailroadResolver
    {
        public Task<ControlNumber?> ResolveFromWorkAreaAsync(
            IOrchestrationUnitOfWork uow,
            ControlNumber workAreaGroupCtrlNbr,
            CancellationToken ct = default) => Task.FromResult<ControlNumber?>(railroadCtrlNbr);

        public ControlNumber? ResolveFromGroup(DynamicGroup? group)
            => railroadCtrlNbr;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Fake infrastructure
    // ═══════════════════════════════════════════════════════════════════════════

    private sealed class FakeUowFactory(FakeOrchestrationUnitOfWork uow) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(OrchestrationUnitOfWorkOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(uow);
    }

    private sealed class FakeDepartmentReassignmentRuleRepo(DepartmentReassignmentRule? rule)
        : FakeRepoBase<DepartmentReassignmentRule>, IDepartmentReassignmentRuleRepository
    {
        public Task<DepartmentReassignmentRule?> GetByDepartmentAsync(ControlNumber departmentCtrlNbr)
            => Task.FromResult(rule);
    }

    private sealed class FakeCraftRepo(Craft craft) : FakeRepoBase<Craft>, ICraftRepository
    {
        public override Task<List<Craft>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(new List<Craft> { craft });

        public override Task<Craft?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<Craft?>(craft.CtrlNbr == ctrlNbr ? craft : null);

        public Task<List<Craft>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? dynamicGroupCtrlNbr) => Task.FromResult(new List<Craft>());
        public Task<List<Craft>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs) => Task.FromResult(new List<Craft>());
        public Task<List<Craft>> GetByDepartmentAsync(ControlNumber departmentCtrlNbr, CancellationToken ct = default) => Task.FromResult(new List<Craft>());
    }

    private sealed class FakeRosterBoardRepo(RosterBoard board) : FakeRepoBase<RosterBoard>, IRosterBoardRepository
    {
        public Task<IReadOnlyList<RosterBoard>> GetActiveByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RosterBoard>>(new List<RosterBoard> { board });

        public Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RosterBoard>>(new List<RosterBoard> { board });

        public Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> craftCtrlNbrs, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RosterBoard>>(new List<RosterBoard> { board });

        public Task<RosterBoard?> GetByPositionCtrlNbrAsync(ControlNumber positionCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<RosterBoard?>(null);

        public Task<RosterBoard?> GetByStaffablePositionCtrlNbrAsync(ControlNumber staffablePositionCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<RosterBoard?>(null);
    }

    /// <summary>
    /// Spy for the canonical repost policy. Records every vacated position handed to it so tests
    /// can assert the crew-vacate path delegates to the single bulletin-producing service instead
    /// of bulletining inline.
    /// </summary>
    private sealed class RecordingVacancyRepostService : IVacancyRepostService
    {
        public List<ControlNumber> RepostedPositions { get; } = [];

        public Task RepostVacatedPositionAsync(
            ControlNumber staffablePositionCtrlNbr,
            ControlNumber? previousIncumbentCtrlNbr = null,
            CancellationToken ct = default)
        {
            RepostedPositions.Add(staffablePositionCtrlNbr);
            return Task.CompletedTask;
        }

        public Task RepostBoardPositionIfUnderstaffedAsync(
            ControlNumber boardCtrlNbr,
            ControlNumber vacatedStaffablePositionCtrlNbr,
            ControlNumber? previousIncumbentCtrlNbr = null,
            CancellationToken ct = default)
        {
            RepostedPositions.Add(vacatedStaffablePositionCtrlNbr);
            return Task.CompletedTask;
        }

        public Task<int> ReconcileUnbulletinedVacantPositionsAsync(CancellationToken ct = default)
            => Task.FromResult(0);
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

    private sealed class FakeCrewPositionRepo(CrewPosition? pos) : FakeRepoBase<CrewPosition>, ICrewPositionRepository
    {
        public override Task<CrewPosition?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(pos);
        public Task<List<CrewPosition>> GetByCrewAsync(ControlNumber c) => Task.FromResult(new List<CrewPosition>());
        public Task<List<CrewPosition>> GetByCrewsAsync(IEnumerable<ControlNumber> cs) => Task.FromResult(new List<CrewPosition>());
        public Task<CrewPosition?> GetByStaffablePositionAsync(ControlNumber s) => Task.FromResult<CrewPosition?>(null);
        public Task<List<ControlNumber>> GetVacantStaffablePositionCtrlNbrsAsync(CancellationToken ct = default) => Task.FromResult(new List<ControlNumber>());
    }

    private sealed class FakeCrewIncumbencyRepo(CrewIncumbency? incumbency) : FakeRepoBase<CrewIncumbency>, ICrewIncumbencyRepository
    {
        public override Task<CrewIncumbency?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(incumbency);
        public Task<List<CrewIncumbency>> GetByCrewPositionAsync(ControlNumber p) => Task.FromResult(new List<CrewIncumbency>());
        public Task<List<CrewIncumbency>> GetActiveByEmployeeAsync(ControlNumber e, DateTime d) => Task.FromResult(new List<CrewIncumbency>());
        public Task<CrewIncumbency?> GetActiveByPositionAsync(ControlNumber p, DateTime d) => Task.FromResult<CrewIncumbency?>(null);
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

    private sealed class FakeEmployeeNotificationRepo : FakeRepoBase<EmployeeNotification>, IEmployeeNotificationRepository
    {
        public Task<List<EmployeeNotification>> GetByEmployeeAsync(ControlNumber e, CancellationToken ct = default) => Task.FromResult(new List<EmployeeNotification>());
        public Task<List<EmployeeNotification>> GetUnacknowledgedByEmployeeAsync(ControlNumber e, CancellationToken ct = default) => Task.FromResult(new List<EmployeeNotification>());
        public Task<List<EmployeeNotification>> GetByRailroadAsync(ControlNumber r, CancellationToken ct = default) => Task.FromResult(new List<EmployeeNotification>());
        public Task<int> CountUnacknowledgedByRailroadAsync(ControlNumber r, CancellationToken ct = default) => Task.FromResult(0);
    }

    private sealed class FakeNotificationTypeConfigRepo : FakeRepoBase<NotificationTypeConfig>, INotificationTypeConfigRepository
    {
        public Task<List<NotificationTypeConfig>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(new List<NotificationTypeConfig>
            {
                NotificationTypeConfig.Create(
                    railroadCtrlNbr,
                    NotificationCategories.BoardPlacement,
                    "Board Placement",
                    isEnabled: true,
                    requiresAcknowledgementDefault: true,
                    messageTemplate: "You have been placed on {board}.")
            });

        public Task<NotificationTypeConfig?> GetByRailroadAndKeyAsync(ControlNumber railroadCtrlNbr, string key, CancellationToken ct = default)
            => Task.FromResult<NotificationTypeConfig?>(
                string.Equals(key, NotificationCategories.BoardPlacement, StringComparison.Ordinal)
                    ? NotificationTypeConfig.Create(
                        railroadCtrlNbr,
                        NotificationCategories.BoardPlacement,
                        "Board Placement",
                        isEnabled: true,
                        requiresAcknowledgementDefault: true,
                        messageTemplate: "You have been placed on {board}.")
                    : null);
    }

    private sealed class FakeVacancyRepo : FakeRepoBase<PositionVacancy>, IPositionVacancyRepository
    {
        public Task<List<PositionVacancy>> GetOpenAsync() => Task.FromResult(new List<PositionVacancy>());
        public Task<List<PositionVacancy>> GetOpenByRailroadAsync(ControlNumber r) => Task.FromResult(new List<PositionVacancy>());
        public Task<List<PositionVacancy>> GetByTargetAsync(string t, ControlNumber c) => Task.FromResult(new List<PositionVacancy>());
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

    private sealed class FakePositionAssignmentRepo(PositionAssignment? assignment) : FakeRepoBase<PositionAssignment>, IPositionAssignmentRepository
    {
        public Task<PositionAssignment?> GetByStaffablePositionAsync(ControlNumber staffablePositionCtrlNbr)
            => Task.FromResult(assignment);
        public Task<List<PositionAssignment>> GetByStaffablePositionsAsync(IEnumerable<ControlNumber> ctrlNbrs) => Task.FromResult(new List<PositionAssignment>());
        public Task<List<PositionAssignment>> GetByEmployeeAsync(ControlNumber e) => Task.FromResult(new List<PositionAssignment>());
        public Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsAsync() => Task.FromResult(new HashSet<long>());
        public Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsByTypeAsync(string t) => Task.FromResult(new HashSet<long>());
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

    private sealed class FakeRosterRepo(Roster? roster) : FakeRepoBase<Roster>, IRosterRepository
    {
        public override Task<Roster?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(roster);

        public Task<List<Roster>> GetByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult(roster is not null ? new List<Roster> { roster } : new List<Roster>());

        public Task<List<Roster>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> craftCtrlNbrs)
            => Task.FromResult(roster is not null ? new List<Roster> { roster } : new List<Roster>());

        public Task<List<Roster>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs, CancellationToken ct = default)
            => Task.FromResult(roster is not null ? new List<Roster> { roster } : new List<Roster>());

        public Task<Roster?> GetTrainingRosterByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<Roster?>(null);
    }

    private sealed class FakeOrchestrationUnitOfWork : IOrchestrationUnitOfWork
    {
        public bool Committed { get; private set; }

        public FakeVacancyRepo            FakeVacancies          { get; } = new();
        public FakeBulletinRepo           FakeBulletins          { get; } = new();
        public FakeEmployeeNotificationRepo FakeEmployeeNotifications { get; } = new();
        public FakePositionAssignmentRepo FakePositionAssignments { get; }
        private readonly FakeCrewRepo            _crews;
        private readonly FakeCrewPositionRepo     _crewPositions;
        private readonly FakeCrewIncumbencyRepo   _incumbencies;
        private readonly FakeCraftRoleRepo        _craftRoles;
        private readonly FakeCraftRepo            _crafts;
        private readonly FakeBulletinRuleRepo     _bulletinRules;
        private readonly FakeDepartmentReassignmentRuleRepo _departmentReassignmentRules;
        private readonly FakeRosterBoardRepo      _rosterBoards;
        private readonly FakeRosterRepo          _rosters;
        private readonly FakeStaffablePositionRepo _staffablePositions = new();
        private readonly FakeDynamicGroupRepo    _dynamicGroups;
        private readonly FakeEmployeeNotificationRepo _employeeNotifications = new();
        private readonly FakeNotificationTypeConfigRepo _notificationTypeConfigs = new();

        public FakeOrchestrationUnitOfWork(
            BulletinRule?        bulletinRule,
            CraftRole?           craftRole,
            Crew?                crew,
            Craft?               craft,
            DepartmentReassignmentRule? departmentReassignmentRule,
            CrewPosition?        crewPosition      = null,
            CrewIncumbency?      incumbency        = null,
            PositionAssignment?  positionAssignment = null)
        {
            _crews          = new FakeCrewRepo(crew);
            _crewPositions  = new FakeCrewPositionRepo(crewPosition);
            _incumbencies   = new FakeCrewIncumbencyRepo(incumbency);
            _craftRoles     = new FakeCraftRoleRepo(craftRole);
            _crafts         = new FakeCraftRepo(craft ?? MakeCraft());
            _bulletinRules  = new FakeBulletinRuleRepo(bulletinRule);
            _departmentReassignmentRules = new FakeDepartmentReassignmentRuleRepo(departmentReassignmentRule);
            _rosterBoards   = new FakeRosterBoardRepo(RosterBoard.Create(CraftCtrlNbr, ControlNumber.Create(999), "Hangout", BoardType.Hangout));
            _rosters        = new FakeRosterRepo(Roster.Create(CraftCtrlNbr, WorkAreaCtrlNbr, null, "Hangout Roster", "Hangout Rosters", 1));
            _dynamicGroups  = new FakeDynamicGroupRepo(
                DynamicGroup.Create(
                    groupTypeCtrlNbr: ControlNumber.Create(1000),
                    name: "Work Area",
                    parentGroupCtrlNbr: null,
                    path: null,
                    isWorkArea: true,
                    code: "WA",
                    parentCtrlNbr: null,
                    railroadCtrlNbr: ControlNumber.Create(999),
                    timeZoneId: null,
                    workPeriodMode: null));
            FakePositionAssignments = new FakePositionAssignmentRepo(positionAssignment);
        }

        public string CorrelationId  => "test";
        public string OrchestrationId => "test";

        public ICrewRepository             Crews              => _crews;
        public ICrewPositionRepository     CrewPositions      => _crewPositions;
        public ICrewIncumbencyRepository   CrewIncumbencies   => _incumbencies;
        public ICraftRoleRepository        CraftRoles         => _craftRoles;
        public ICraftRepository            Crafts             => _crafts;
        public IBulletinRuleRepository     BulletinRules      => _bulletinRules;
        public IDepartmentReassignmentRuleRepository DepartmentReassignmentRules => _departmentReassignmentRules;
        public IRosterBoardRepository      RosterBoards       => _rosterBoards;
        public IPositionVacancyRepository  PositionVacancies  => FakeVacancies;
        public IBulletinRepository         Bulletins          => FakeBulletins;
        public IPositionAssignmentRepository PositionAssignments => FakePositionAssignments;
        public IStaffablePositionRepository StaffablePositions  => _staffablePositions;
        public IEmployeeNotificationRepository EmployeeNotifications => FakeEmployeeNotifications;
        public INotificationTypeConfigRepository NotificationTypeConfigs => _notificationTypeConfigs;

        public Task CommitAsync(CancellationToken ct = default) { Committed = true; return Task.CompletedTask; }
        public Task SaveAsync(CancellationToken ct = default)   => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }

        // ── unused interface members ──────────────────────────────────────────
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken ct = default) => Task.CompletedTask;
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
        public IAbsenceApprovalPolicyRepository          AbsenceApprovalPolicies      => throw new NotImplementedException();
        public ICallSheetRuleRepository                  CallSheetRules              => throw new NotImplementedException();
        public ICraftCallSheetRuleRepository CraftCallSheetRules => null!;
        public ISeniorityMovePolicyRepository            SeniorityMovePolicies        => throw new NotImplementedException();
        public ISeniorityMoveRepository                  SeniorityMoves               => new NoOpSeniorityMoveRepository();
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
        public IRosterRepository                         Rosters                      => _rosters;
        public ISeniorityStateRepository                 SeniorityStates              => null!;
        public IGroupTypeRepository                      GroupTypes                   => null!;
        public IDynamicGroupRepository                   DynamicGroups                => _dynamicGroups;
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
