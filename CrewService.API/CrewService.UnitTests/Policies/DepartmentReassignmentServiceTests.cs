using CrewService.Application.Policies;
using CrewService.Application.Notifications;
using CrewService.Application.TenantConfig;
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

namespace CrewService.UnitTests.Policies;

public class DepartmentReassignmentServiceTests
{
    private static readonly ControlNumber DepartmentCtrlNbr = ControlNumber.Create(10);
    private static readonly ControlNumber EmployeeCtrlNbr = ControlNumber.Create(20);
    private static readonly ControlNumber CraftCtrlNbr = ControlNumber.Create(30);

    [Fact]
    public async Task ReassignEmployeeAsync_WhenRuleMissing_Throws()
    {
        var uow = BuildUow(
            rule: null,
            crafts: [MakeCraft(DepartmentCtrlNbr)],
            boardsByCraft: new Dictionary<ControlNumber, IReadOnlyList<RosterBoard>>());

        var sut = new DepartmentReassignmentService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ReassignEmployeeAsync(uow, EmployeeCtrlNbr, DepartmentCtrlNbr, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReassignEmployeeAsync_WhenRequiredAndNoBoard_Throws()
    {
        var rule = DepartmentReassignmentRule.Create(DepartmentCtrlNbr, BoardType.Hangout, isRequired: true);
        var uow = BuildUow(
            rule: rule,
            crafts: [MakeCraft(DepartmentCtrlNbr)],
            boardsByCraft: new Dictionary<ControlNumber, IReadOnlyList<RosterBoard>>());

        var sut = new DepartmentReassignmentService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ReassignEmployeeAsync(uow, EmployeeCtrlNbr, DepartmentCtrlNbr, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReassignEmployeeAsync_WhenEmployeeAlreadyOnTargetBoard_NoAdds()
    {
        var craft = MakeCraft(DepartmentCtrlNbr);
        var rule = DepartmentReassignmentRule.Create(DepartmentCtrlNbr, BoardType.Hangout, isRequired: true);
        var board = RosterBoard.Create(craft.CtrlNbr, ControlNumber.Create(99), "Hangout", BoardType.Hangout, isActive: true);
        var existing = StaffablePosition.Create(StaffablePositionType.Board);
        board.AddPosition(EmployeeCtrlNbr, 1, existing.CtrlNbr);

        var uow = BuildUow(
            rule: rule,
            crafts: [craft],
            boardsByCraft: new Dictionary<ControlNumber, IReadOnlyList<RosterBoard>>
            {
                [craft.CtrlNbr] = [board]
            });

        var sut = new DepartmentReassignmentService();

        await sut.ReassignEmployeeAsync(uow, EmployeeCtrlNbr, DepartmentCtrlNbr, TestContext.Current.CancellationToken);

        Assert.Empty(uow.StaffableRepo.AddedEntities);
        Assert.Empty(uow.AssignmentRepo.AddedEntities);
    }

    [Fact]
    public async Task ReassignEmployeeAsync_WhenRuleOptionalAndNoBoard_NoAdds()
    {
        var rule = DepartmentReassignmentRule.Create(DepartmentCtrlNbr, BoardType.Hangout, isRequired: false);
        var uow = BuildUow(
            rule: rule,
            crafts: [MakeCraft(DepartmentCtrlNbr)],
            boardsByCraft: new Dictionary<ControlNumber, IReadOnlyList<RosterBoard>>());

        var sut = new DepartmentReassignmentService();

        await sut.ReassignEmployeeAsync(uow, EmployeeCtrlNbr, DepartmentCtrlNbr, TestContext.Current.CancellationToken);

        Assert.Empty(uow.StaffableRepo.AddedEntities);
        Assert.Empty(uow.AssignmentRepo.AddedEntities);
    }

    [Fact]
    public async Task ReassignEmployeeAsync_WhenRuleAndBoardExist_AddsBoardPositionAndAssignment()
    {
        var craft = MakeCraft(DepartmentCtrlNbr);
        var rule = DepartmentReassignmentRule.Create(DepartmentCtrlNbr, BoardType.Hangout, isRequired: true);
        var board = RosterBoard.Create(craft.CtrlNbr, ControlNumber.Create(99), "Hangout", BoardType.Hangout, isActive: true);

        var uow = BuildUow(
            rule: rule,
            crafts: [craft],
            boardsByCraft: new Dictionary<ControlNumber, IReadOnlyList<RosterBoard>>
            {
                [craft.CtrlNbr] = [board]
            });

        var sut = new DepartmentReassignmentService();

        await sut.ReassignEmployeeAsync(uow, EmployeeCtrlNbr, DepartmentCtrlNbr, TestContext.Current.CancellationToken);

        Assert.Contains(board.Positions, p => p.EmployeeCtrlNbr == EmployeeCtrlNbr);
        var addedAssignment = Assert.Single(uow.AssignmentRepo.AddedEntities);
        Assert.Equal(EmployeeCtrlNbr, addedAssignment.EmployeeCtrlNbr);
        Assert.Equal(PositionAssignmentType.Board, addedAssignment.AssignmentType);
    }

    [Fact]
    public async Task ReassignEmployeeAsync_WhenBoardNotifyOnPlacementEnabled_AddsNotification()
    {
        var workAreaCtrlNbr = ControlNumber.Create(40);
        var railroadCtrlNbr = ControlNumber.Create(50);
        var roster = Roster.Create(CraftCtrlNbr, workAreaCtrlNbr, null, "Trainman", "Trainmen", 1);
        var craft = MakeCraft(DepartmentCtrlNbr);
        var rule = DepartmentReassignmentRule.Create(DepartmentCtrlNbr, BoardType.Hangout, isRequired: true);
        var board = RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr, "Hangout", BoardType.Hangout, isActive: true);
        board.SetNotifyOnPlacement(true);
        board.SetPlacementRequiresAcknowledgement(true);

        var uow = BuildUow(
            rule: rule,
            crafts: [craft],
            boardsByCraft: new Dictionary<ControlNumber, IReadOnlyList<RosterBoard>>
            {
                [craft.CtrlNbr] = [board]
            },
            rostersByCtrlNbr: new Dictionary<ControlNumber, Roster>
            {
                [roster.CtrlNbr] = roster
            },
            dynamicGroupsByCtrlNbr: new Dictionary<ControlNumber, DynamicGroup>
            {
                [workAreaCtrlNbr] = DynamicGroup.Create(
                    groupTypeCtrlNbr: ControlNumber.Create(900),
                    name: "Work Area",
                    parentGroupCtrlNbr: null,
                    path: null,
                    isWorkArea: true,
                    code: "WA",
                    parentCtrlNbr: null,
                    railroadCtrlNbr: railroadCtrlNbr,
                    timeZoneId: null,
                    workPeriodMode: null)
            });

        var notifications = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            new RailroadResolver(),
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));

        var sut = new DepartmentReassignmentService(notifications);

        await sut.ReassignEmployeeAsync(uow, EmployeeCtrlNbr, DepartmentCtrlNbr, TestContext.Current.CancellationToken);

        var notification = Assert.Single(uow.EmployeeNotificationRepo.AddedEntities);
        Assert.Equal(EmployeeCtrlNbr, notification.EmployeeCtrlNbr);
        Assert.Equal(railroadCtrlNbr, notification.RailroadCtrlNbr);
        Assert.Equal(NotificationCategories.BoardPlacement, notification.Category);
        Assert.True(notification.RequiresAcknowledgement);
    }

    [Fact]
    public async Task ReassignEmployeeAsync_PrefersEmployeeCurrentCraftBoard_WhenMultipleCraftBoardsExist()
    {
        var employeeCraft = MakeCraft(DepartmentCtrlNbr);
        var otherCraft = MakeCraft(DepartmentCtrlNbr);
        var employeeCraftRole = CraftRole.Create(employeeCraft.CtrlNbr, "TR", "Trainman");
        var otherCraftRole = CraftRole.Create(otherCraft.CtrlNbr, "EN", "Engineer");

        var employeeBoard = RosterBoard.Create(employeeCraft.CtrlNbr, ControlNumber.Create(101), "Trainman Hangout", BoardType.Hangout, isActive: true);
        var otherBoard = RosterBoard.Create(otherCraft.CtrlNbr, ControlNumber.Create(102), "Engineer Hangout", BoardType.Hangout, isActive: true);

        var currentStaffable = StaffablePosition.Create(StaffablePositionType.Crew);
        var currentCrewPosition = CrewPosition.Create(ControlNumber.Create(500), employeeCraftRole.CtrlNbr, 1, currentStaffable.CtrlNbr);
        var currentAssignment = PositionAssignment.Create(
            currentStaffable.CtrlNbr,
            EmployeeCtrlNbr,
            PositionAssignmentType.Direct,
            currentCrewPosition.CtrlNbr,
            assignedDateUtc: DateTime.UtcNow.AddHours(-2));

        var rule = DepartmentReassignmentRule.Create(DepartmentCtrlNbr, BoardType.Hangout, isRequired: true);
        var uow = BuildUow(
            rule: rule,
            crafts: [employeeCraft, otherCraft],
            boardsByCraft: new Dictionary<ControlNumber, IReadOnlyList<RosterBoard>>
            {
                [employeeCraft.CtrlNbr] = [employeeBoard],
                [otherCraft.CtrlNbr] = [otherBoard]
            },
            craftRolesByCtrlNbr: new Dictionary<ControlNumber, CraftRole>
            {
                [employeeCraftRole.CtrlNbr] = employeeCraftRole,
                [otherCraftRole.CtrlNbr] = otherCraftRole
            },
            crewPositionsByCtrlNbr: new Dictionary<ControlNumber, CrewPosition>
            {
                [currentCrewPosition.CtrlNbr] = currentCrewPosition
            },
            assignmentsByEmployee: new Dictionary<ControlNumber, IReadOnlyList<PositionAssignment>>
            {
                [EmployeeCtrlNbr] = [currentAssignment]
            });

        var sut = new DepartmentReassignmentService();

        await sut.ReassignEmployeeAsync(uow, EmployeeCtrlNbr, DepartmentCtrlNbr, TestContext.Current.CancellationToken);

        Assert.Contains(employeeBoard.Positions, p => p.EmployeeCtrlNbr == EmployeeCtrlNbr);
        Assert.DoesNotContain(otherBoard.Positions, p => p.EmployeeCtrlNbr == EmployeeCtrlNbr);
    }

    private static Craft MakeCraft(ControlNumber departmentCtrlNbr) =>
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
            departmentCtrlNbr: departmentCtrlNbr);

    private static FakeOrchestrationUnitOfWork BuildUow(
        DepartmentReassignmentRule? rule,
        IReadOnlyList<Craft> crafts,
        IReadOnlyDictionary<ControlNumber, IReadOnlyList<RosterBoard>> boardsByCraft,
        IReadOnlyDictionary<ControlNumber, CraftRole>? craftRolesByCtrlNbr = null,
        IReadOnlyDictionary<ControlNumber, CrewPosition>? crewPositionsByCtrlNbr = null,
        IReadOnlyDictionary<ControlNumber, IReadOnlyList<PositionAssignment>>? assignmentsByEmployee = null,
        IReadOnlyDictionary<ControlNumber, Roster>? rostersByCtrlNbr = null,
        IReadOnlyDictionary<ControlNumber, DynamicGroup>? dynamicGroupsByCtrlNbr = null)
    {
        return new FakeOrchestrationUnitOfWork(
            rule,
            crafts,
            boardsByCraft,
            craftRolesByCtrlNbr,
            crewPositionsByCtrlNbr,
            assignmentsByEmployee,
            rostersByCtrlNbr,
            dynamicGroupsByCtrlNbr);
    }

    private abstract class RepoBase<TEntity> : IRepository<TEntity> where TEntity : Entity
    {
        public List<TEntity> AddedEntities { get; } = [];
        public virtual Task<List<TEntity>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<TEntity>());
        public virtual Task<List<TEntity>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<TEntity>());
        public virtual Task<TEntity?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<TEntity?>(null);
        public virtual Task<TEntity?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<TEntity?>(null);
        public virtual Task AddAsync(TEntity entity, CancellationToken ct = default) { AddedEntities.Add(entity); return Task.CompletedTask; }
        public virtual Task UpdateAsync(TEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual void Add(TEntity entity) => AddedEntities.Add(entity);
        public virtual void Update(TEntity entity) { }
        public virtual void Remove(TEntity entity) { }
    }

    private sealed class FakeDepartmentRuleRepo(DepartmentReassignmentRule? rule) : RepoBase<DepartmentReassignmentRule>, IDepartmentReassignmentRuleRepository
    {
        public Task<DepartmentReassignmentRule?> GetByDepartmentAsync(ControlNumber departmentCtrlNbr) => Task.FromResult(rule);
    }

    private sealed class FakeCraftRepo(IReadOnlyList<Craft> crafts) : RepoBase<Craft>, ICraftRepository
    {
        public override Task<List<Craft>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(crafts.ToList());
        public Task<List<Craft>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr) => Task.FromResult(new List<Craft>());
        public Task<List<Craft>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs) => Task.FromResult(new List<Craft>());
        public Task<List<Craft>> GetByDepartmentAsync(ControlNumber departmentCtrlNbr, CancellationToken ct = default) => Task.FromResult(new List<Craft>());
    }

    private sealed class FakeBoardRepo(IReadOnlyDictionary<ControlNumber, IReadOnlyList<RosterBoard>> boardsByCraft) : RepoBase<RosterBoard>, IRosterBoardRepository
    {
        public Task<IReadOnlyList<RosterBoard>> GetActiveByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RosterBoard>>(boardsByCraft.Values.SelectMany(x => x).Where(b => b.IsActive).ToList());

        public Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(boardsByCraft.TryGetValue(craftCtrlNbr, out var boards)
                ? boards
                : (IReadOnlyList<RosterBoard>)Array.Empty<RosterBoard>());

        public Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> craftCtrlNbrs, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RosterBoard>>(boardsByCraft
                .Where(kvp => craftCtrlNbrs.Contains(kvp.Key))
                .SelectMany(kvp => kvp.Value)
                .ToList());

        public Task<RosterBoard?> GetByPositionCtrlNbrAsync(ControlNumber positionCtrlNbr, CancellationToken ct = default) => Task.FromResult<RosterBoard?>(null);
        public Task<RosterBoard?> GetByStaffablePositionCtrlNbrAsync(ControlNumber staffablePositionCtrlNbr, CancellationToken ct = default) => Task.FromResult<RosterBoard?>(null);
    }

    private sealed class FakeStaffableRepo : RepoBase<StaffablePosition>, IStaffablePositionRepository
    {
        public Task<List<StaffablePosition>> GetByPositionTypeAsync(string positionType) => Task.FromResult(new List<StaffablePosition>());
    }

    private sealed class FakeAssignmentRepo(
        IReadOnlyDictionary<ControlNumber, IReadOnlyList<PositionAssignment>> assignmentsByEmployee) : RepoBase<PositionAssignment>, IPositionAssignmentRepository
    {
        public Task<PositionAssignment?> GetByStaffablePositionAsync(ControlNumber staffablePositionCtrlNbr) => Task.FromResult<PositionAssignment?>(null);
        public Task<List<PositionAssignment>> GetByStaffablePositionsAsync(IEnumerable<ControlNumber> ctrlNbrs) => Task.FromResult(new List<PositionAssignment>());
        public Task<List<PositionAssignment>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr)
            => Task.FromResult(assignmentsByEmployee.TryGetValue(employeeCtrlNbr, out var assignments)
                ? assignments.ToList()
                : new List<PositionAssignment>());
        public Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsAsync() => Task.FromResult(new HashSet<long>());
        public Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsByTypeAsync(string positionType) => Task.FromResult(new HashSet<long>());
    }

    private sealed class FakeCraftRoleRepo(IReadOnlyDictionary<ControlNumber, CraftRole> craftRolesByCtrlNbr)
        : RepoBase<CraftRole>, ICraftRoleRepository
    {
        public override Task<CraftRole?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(craftRolesByCtrlNbr.TryGetValue(ctrlNbr, out var craftRole) ? craftRole : null);

        public Task<List<CraftRole>> GetByCraftAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult(craftRolesByCtrlNbr.Values.Where(cr => cr.CraftCtrlNbr == craftCtrlNbr).ToList());

        public Task<List<CraftRole>> GetByDepartmentAsync(ControlNumber departmentCtrlNbr)
            => Task.FromResult(new List<CraftRole>());

        public Task<List<CraftRole>> GetByRailroadAsync(ControlNumber railroadCtrlNbr)
            => Task.FromResult(new List<CraftRole>());

        public Task<CraftRole?> GetByCtrlNbrWithQualificationsAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => GetByCtrlNbrAsync(ctrlNbr, ct);
    }

    private sealed class FakeCrewPositionRepo(IReadOnlyDictionary<ControlNumber, CrewPosition> crewPositionsByCtrlNbr)
        : RepoBase<CrewPosition>, ICrewPositionRepository
    {
        public override Task<CrewPosition?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(crewPositionsByCtrlNbr.TryGetValue(ctrlNbr, out var crewPosition) ? crewPosition : null);

        public Task<List<CrewPosition>> GetByCrewAsync(ControlNumber crewCtrlNbr)
            => Task.FromResult(crewPositionsByCtrlNbr.Values.Where(cp => cp.CrewCtrlNbr == crewCtrlNbr).ToList());

        public Task<List<CrewPosition>> GetByCrewsAsync(IEnumerable<ControlNumber> crewCtrlNbrs)
        {
            var set = crewCtrlNbrs.ToHashSet();
            return Task.FromResult(crewPositionsByCtrlNbr.Values.Where(cp => set.Contains(cp.CrewCtrlNbr)).ToList());
        }

        public Task<CrewPosition?> GetByStaffablePositionAsync(ControlNumber staffablePositionCtrlNbr)
            => Task.FromResult(crewPositionsByCtrlNbr.Values.FirstOrDefault(cp => cp.StaffablePositionCtrlNbr == staffablePositionCtrlNbr));

        public Task<List<ControlNumber>> GetVacantStaffablePositionCtrlNbrsAsync(CancellationToken ct = default)
            => Task.FromResult(new List<ControlNumber>());
    }

    private sealed class FakeRosterRepo(IReadOnlyDictionary<ControlNumber, Roster> rostersByCtrlNbr)
        : RepoBase<Roster>, IRosterRepository
    {
        public override Task<Roster?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(rostersByCtrlNbr.TryGetValue(ctrlNbr, out var roster) ? roster : null);

        public Task<List<Roster>> GetByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult(rostersByCtrlNbr.Values.Where(r => r.CraftCtrlNbr == craftCtrlNbr).ToList());

        public Task<List<Roster>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> craftCtrlNbrs)
        {
            var set = craftCtrlNbrs.ToHashSet();
            return Task.FromResult(rostersByCtrlNbr.Values.Where(r => set.Contains(r.CraftCtrlNbr)).ToList());
        }

        public Task<List<Roster>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs, CancellationToken ct = default)
        {
            var set = ctrlNbrs.ToHashSet();
            return Task.FromResult(rostersByCtrlNbr.Where(kvp => set.Contains(kvp.Key)).Select(kvp => kvp.Value).ToList());
        }

        public Task<Roster?> GetTrainingRosterByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<Roster?>(null);
    }

    private sealed class FakeDynamicGroupRepo(IReadOnlyDictionary<ControlNumber, DynamicGroup> groupsByCtrlNbr)
        : RepoBase<DynamicGroup>, IDynamicGroupRepository
    {
        public override Task<DynamicGroup?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(groupsByCtrlNbr.TryGetValue(ctrlNbr, out var group) ? group : null);

        public Task<List<DynamicGroup>> GetByParentCtrlNbrAsync(ControlNumber? parentGroupCtrlNbr)
            => Task.FromResult(new List<DynamicGroup>());

        public Task<List<DynamicGroup>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs)
            => Task.FromResult(groupsByCtrlNbr.Where(kvp => ctrlNbrs.Contains(kvp.Key)).Select(kvp => kvp.Value).ToList());

        public Task<DynamicGroup?> GetByGroupTypeAndNameIncludingDeletedAsync(ControlNumber groupTypeCtrlNbr, string name)
            => Task.FromResult<DynamicGroup?>(null);

        public Task<List<DynamicGroup>> GetWorkAreasAsync(ControlNumber? railroadCtrlNbr = null)
            => Task.FromResult(new List<DynamicGroup>());

        public Task<List<DynamicGroup>> GetWorkAreasWithDescendantsAsync()
            => Task.FromResult(new List<DynamicGroup>());

        public Task<List<DynamicGroup>> GetAncestorsAsync(ControlNumber groupCtrlNbr)
            => Task.FromResult(new List<DynamicGroup>());

        public Task<List<DynamicGroup>> GetTreeAsync(ControlNumber? rootCtrlNbr = null)
            => Task.FromResult(new List<DynamicGroup>());

        public Task<List<DynamicGroup>> GetByGroupTypeNameAsync(string typeName, ControlNumber? parentCtrlNbr = null)
            => Task.FromResult(new List<DynamicGroup>());

        public Task BackfillPathsAsync() => Task.CompletedTask;
    }

    private sealed class FakeEmployeeNotificationRepo : RepoBase<EmployeeNotification>, IEmployeeNotificationRepository
    {
        public Task<List<EmployeeNotification>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(new List<EmployeeNotification>());

        public Task<List<EmployeeNotification>> GetUnacknowledgedByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(new List<EmployeeNotification>());

        public Task<List<EmployeeNotification>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(new List<EmployeeNotification>());

        public Task<int> CountUnacknowledgedByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(0);
    }

    private sealed class FakeNotificationTypeConfigRepo : RepoBase<NotificationTypeConfig>, INotificationTypeConfigRepository
    {
        public Task<List<NotificationTypeConfig>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(new List<NotificationTypeConfig>
            {
                NotificationTypeConfig.Create(
                    railroadCtrlNbr,
                    NotificationCategories.BoardPlacement,
                    "Board Placement",
                    isEnabled: true,
                    requiresAcknowledgementDefault: true)
            });

        public Task<NotificationTypeConfig?> GetByRailroadAndKeyAsync(ControlNumber railroadCtrlNbr, string key, CancellationToken ct = default)
            => Task.FromResult<NotificationTypeConfig?>(
                string.Equals(key, NotificationCategories.BoardPlacement, StringComparison.Ordinal)
                    ? NotificationTypeConfig.Create(
                        railroadCtrlNbr,
                        NotificationCategories.BoardPlacement,
                        "Board Placement",
                        isEnabled: true,
                        requiresAcknowledgementDefault: true)
                    : null);
    }

    private sealed class FakeOrchestrationUnitOfWork : IOrchestrationUnitOfWork
    {
        public FakeDepartmentRuleRepo RuleRepo { get; }
        public FakeCraftRepo CraftRepo { get; }
        public FakeBoardRepo BoardRepo { get; }
        public FakeCraftRoleRepo CraftRoleRepo { get; }
        public FakeCrewPositionRepo CrewPositionRepo { get; }
        public FakeRosterRepo RosterRepo { get; }
        public FakeDynamicGroupRepo DynamicGroupRepo { get; }
        public FakeEmployeeNotificationRepo EmployeeNotificationRepo { get; } = new();
        public FakeStaffableRepo StaffableRepo { get; } = new();
        public FakeAssignmentRepo AssignmentRepo { get; }
        private readonly FakeNotificationTypeConfigRepo _notificationTypeConfigs = new();

        public FakeOrchestrationUnitOfWork(
            DepartmentReassignmentRule? rule,
            IReadOnlyList<Craft> crafts,
            IReadOnlyDictionary<ControlNumber, IReadOnlyList<RosterBoard>> boardsByCraft,
            IReadOnlyDictionary<ControlNumber, CraftRole>? craftRolesByCtrlNbr = null,
            IReadOnlyDictionary<ControlNumber, CrewPosition>? crewPositionsByCtrlNbr = null,
            IReadOnlyDictionary<ControlNumber, IReadOnlyList<PositionAssignment>>? assignmentsByEmployee = null,
            IReadOnlyDictionary<ControlNumber, Roster>? rostersByCtrlNbr = null,
            IReadOnlyDictionary<ControlNumber, DynamicGroup>? dynamicGroupsByCtrlNbr = null)
        {
            RuleRepo = new FakeDepartmentRuleRepo(rule);
            CraftRepo = new FakeCraftRepo(crafts);
            BoardRepo = new FakeBoardRepo(boardsByCraft);
            CraftRoleRepo = new FakeCraftRoleRepo(craftRolesByCtrlNbr ?? new Dictionary<ControlNumber, CraftRole>());
            CrewPositionRepo = new FakeCrewPositionRepo(crewPositionsByCtrlNbr ?? new Dictionary<ControlNumber, CrewPosition>());
            RosterRepo = new FakeRosterRepo(rostersByCtrlNbr ?? new Dictionary<ControlNumber, Roster>());
            DynamicGroupRepo = new FakeDynamicGroupRepo(dynamicGroupsByCtrlNbr ?? new Dictionary<ControlNumber, DynamicGroup>());
            AssignmentRepo = new FakeAssignmentRepo(assignmentsByEmployee ?? new Dictionary<ControlNumber, IReadOnlyList<PositionAssignment>>());
        }

        public string CorrelationId => "test";
        public string OrchestrationId => "test";
        public IDepartmentReassignmentRuleRepository DepartmentReassignmentRules => RuleRepo;
        public ICraftRepository Crafts => CraftRepo;
        public IRosterBoardRepository RosterBoards => BoardRepo;
        public IStaffablePositionRepository StaffablePositions => StaffableRepo;
        public IPositionAssignmentRepository PositionAssignments => AssignmentRepo;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }

        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEmployeeRepository Employees => null!;
        public IEmailAddressRepository EmailAddresses => null!;
        public IParentRepository Parents => null!;
        public IUserParentAssignmentRepository UserParentAssignments => null!;
        public IInvitationRepository Invitations => null!;
        public IPayrollTierRepository PayrollTiers => null!;
        public IAddressTypeRepository AddressTypes => null!;
        public IPhoneNumberTypeRepository PhoneNumberTypes => null!;
        public IEmailAddressTypeRepository EmailAddressTypes => null!;
        public IEmploymentStatusRepository EmploymentStatuses => null!;
        public IEmploymentStatusHistoryRepository EmploymentStatusHistory => null!;
        public IEmployeePriorServiceCreditRepository EmployeePriorServiceCredits => null!;
        public IRosterRepository Rosters => RosterRepo;
        public ISeniorityRepository Seniority => null!;
        public ISeniorityStateRepository SeniorityStates => null!;
        public ISeniorityStateVacancyConfigRepository SeniorityStateVacancyConfigs => null!;
        public ISeniorityStateTypeVacancyDefaultRepository SeniorityStateTypeVacancyDefaults => null!;
        public IPendingSeniorityStateChangeRepository PendingSeniorityStateChanges => null!;
        public IGroupTypeRepository GroupTypes => null!;
        public IDynamicGroupRepository DynamicGroups => DynamicGroupRepo;
        public IGroupAttributeDefinitionRepository AttributeDefinitions => null!;
        public IGroupAttributeValueRepository AttributeValues => null!;
        public IBoardCascadePolicyRepository BoardCascadePolicies => null!;
        public IRequiredPositionsStrategyRepository RequiredPositionsStrategies => null!;
        public ICraftRequiredPositionsStrategyRepository CraftRequiredPositionsStrategies => null!;
        public ICrewRepository Crews => null!;
        public ICrewPositionRepository CrewPositions => CrewPositionRepo;
        public ICrewIncumbencyRepository CrewIncumbencies => null!;
        public ICrewAssignmentRepository CrewAssignments => null!;
        public ICrewAttachmentInstanceRepository CrewAttachmentInstances => null!;
        public IAssignmentRepository Assignments => null!;
        public IAssignmentScheduleRepository AssignmentSchedules => null!;
        public IDepartmentRepository Departments => null!;
        public ICraftRoleRepository CraftRoles => CraftRoleRepo;
        public ICraftRoleQualificationRepository CraftRoleQualifications => null!;
        public IWorkInstanceRepository WorkInstances => null!;
        public IPositionSlotRepository PositionSlots => null!;
        public ISlotRequirementRepository SlotRequirements => null!;
        public IShiftDefinitionRepository ShiftDefinitions => null!;
        public IShiftInstanceRepository ShiftInstances => null!;
        public IOnDutyRecordRepository OnDutyRecords => null!;
        public IOffDutyRecordRepository OffDutyRecords => null!;
        public ICraftOperationsPolicyRepository CraftOperationsPolicies => null!;
        public ICraftDisplacementPolicyRepository CraftDisplacementPolicies => null!;
        public IDisplacementCaseRepository DisplacementCases => null!;
        public IDisplacementClaimRepository DisplacementClaims => null!;
        public IBulletinPolicyRepository BulletinPolicies => null!;
        public ICallSheetRuleRepository CallSheetRules => null!;
        public ISeniorityMovePolicyRepository SeniorityMovePolicies => null!;
        public ISeniorityMoveRepository SeniorityMoves => new NoOpSeniorityMoveRepository();
        public IDispatchProjectionRepository DispatchProjections => null!;
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
        public IPositionVacancyRepository PositionVacancies => null!;
        public IBulletinRepository Bulletins => null!;
        public IBulletinBidRepository BulletinBids => null!;
        public IBulletinRuleRepository BulletinRules => null!;
        public IEmployeeNotificationRepository EmployeeNotifications => EmployeeNotificationRepo;
        public INotificationTypeConfigRepository NotificationTypeConfigs => _notificationTypeConfigs;
    }
}
