using CrewService.Application.Notifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employees;
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
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CrewService.UnitTests.Notifications;

public class EmployeeNotificationServiceTests
{
    private static readonly ControlNumber RailroadCtrlNbr = ControlNumber.Create(1);
    private static readonly ControlNumber WorkAreaGroupCtrlNbr = ControlNumber.Create(2);
    private static readonly ControlNumber CraftCtrlNbr = ControlNumber.Create(3);
    private static readonly ControlNumber EmployeeCtrlNbr = ControlNumber.Create(100);
    private static readonly ControlNumber TargetPositionCtrlNbr = ControlNumber.Create(200);

    private static EmployeeNotificationService BuildService() =>
        new(NullLogger<EmployeeNotificationService>.Instance);

    private static DynamicGroup MakeWorkArea() =>
        DynamicGroup.Create(ControlNumber.Create(9), "Houston Yard", parentGroupCtrlNbr: null,
            path: null, isWorkArea: true, railroadCtrlNbr: RailroadCtrlNbr);

    private static PositionVacancy MakeVacancy() =>
        PositionVacancy.Create(WorkAreaGroupCtrlNbr, StaffablePositionType.Crew,
            TargetPositionCtrlNbr, CraftCtrlNbr, "POSITION_CREATED");

    private static Bulletin MakeBulletin(PositionVacancy vacancy) =>
        Bulletin.Create(vacancy.CtrlNbr, CraftCtrlNbr,
            DateTime.UtcNow, DateTime.UtcNow.AddHours(48), DateTime.UtcNow.AddDays(7));

    private static SeniorityMove MakeMove() =>
        SeniorityMove.Create(RailroadCtrlNbr, EmployeeCtrlNbr, CraftCtrlNbr,
            TargetPositionCtrlNbr, displacedEmployeeCtrlNbr: null, daysOnCurrentPosition: 30,
            effectiveUtc: DateTime.UtcNow.AddDays(1));

    // ── Bulletin award ───────────────────────────────────────────────────

    [Fact]
    public async Task NotifyBulletinAwarded_AddsAcknowledgementRequiredNotification()
    {
        var vacancy = MakeVacancy();
        var uow = new FakeNotificationUoW(vacancy, MakeWorkArea());
        var bulletin = MakeBulletin(vacancy);

        await BuildService().NotifyBulletinAwardedAsync(uow, bulletin, EmployeeCtrlNbr,
            forceAssigned: false, TestContext.Current.CancellationToken);

        var notification = Assert.Single(uow.Notifications.AddedEntities);
        Assert.Equal(NotificationCategories.BulletinAward, notification.Category);
        Assert.Equal(EmployeeCtrlNbr, notification.EmployeeCtrlNbr);
        Assert.Equal(RailroadCtrlNbr, notification.RailroadCtrlNbr);
        Assert.True(notification.RequiresAcknowledgement);
    }

    [Fact]
    public async Task NotifyBulletinAwarded_ForceAssigned_UsesForceAssignCategory()
    {
        var vacancy = MakeVacancy();
        var uow = new FakeNotificationUoW(vacancy, MakeWorkArea());
        var bulletin = MakeBulletin(vacancy);

        await BuildService().NotifyBulletinAwardedAsync(uow, bulletin, EmployeeCtrlNbr,
            forceAssigned: true, TestContext.Current.CancellationToken);

        var notification = Assert.Single(uow.Notifications.AddedEntities);
        Assert.Equal(NotificationCategories.ForceAssign, notification.Category);
        Assert.True(notification.RequiresAcknowledgement);
    }

    [Fact]
    public async Task NotifyBulletinAwarded_UnresolvableRailroad_AddsNoNotification()
    {
        var vacancy = MakeVacancy();
        var uow = new FakeNotificationUoW(vacancy, workArea: null);
        var bulletin = MakeBulletin(vacancy);

        await BuildService().NotifyBulletinAwardedAsync(uow, bulletin, EmployeeCtrlNbr,
            forceAssigned: false, TestContext.Current.CancellationToken);

        Assert.Empty(uow.Notifications.AddedEntities);
    }

    // ── Bulletin loss ────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyBulletinLost_AddsInformationalNotification()
    {
        var vacancy = MakeVacancy();
        var uow = new FakeNotificationUoW(vacancy, MakeWorkArea());
        var bulletin = MakeBulletin(vacancy);

        await BuildService().NotifyBulletinLostAsync(uow, bulletin, EmployeeCtrlNbr,
            TestContext.Current.CancellationToken);

        var notification = Assert.Single(uow.Notifications.AddedEntities);
        Assert.False(notification.RequiresAcknowledgement);
        Assert.True(notification.IsAcknowledged);
    }

    // ── Seniority move ───────────────────────────────────────────────────

    [Fact]
    public async Task NotifySeniorityMoveExecuted_AddsAcknowledgementRequiredNotification()
    {
        var uow = new FakeNotificationUoW(vacancy: null, workArea: null);
        var move = MakeMove();

        await BuildService().NotifySeniorityMoveExecutedAsync(uow, move,
            TestContext.Current.CancellationToken);

        var notification = Assert.Single(uow.Notifications.AddedEntities);
        Assert.Equal(NotificationCategories.SeniorityMove, notification.Category);
        Assert.Equal(EmployeeCtrlNbr, notification.EmployeeCtrlNbr);
        Assert.True(notification.RequiresAcknowledgement);
    }

    // ── Displacement ─────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyDisplaced_AddsPositionChangeNotification()
    {
        var uow = new FakeNotificationUoW(vacancy: null, workArea: null);

        await BuildService().NotifyDisplacedAsync(uow, RailroadCtrlNbr, EmployeeCtrlNbr,
            subject: null, TestContext.Current.CancellationToken);

        var notification = Assert.Single(uow.Notifications.AddedEntities);
        Assert.Equal(NotificationCategories.PositionChange, notification.Category);
        Assert.Equal(EmployeeCtrlNbr, notification.EmployeeCtrlNbr);
        Assert.True(notification.RequiresAcknowledgement);
    }
}

// ── Fakes ────────────────────────────────────────────────────────────────

internal abstract class FakeNotificationRepoBase<T> : IRepository<T> where T : Entity
{
    public List<T> AddedEntities { get; } = [];

    public Task<List<T>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<T>());
    public Task<List<T>> GetAllAsync(int page, int size, CancellationToken ct = default) => Task.FromResult(new List<T>());
    public virtual Task<T?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<T?>(null);
    public Task<T?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<T?>(null);
    public Task AddAsync(T entity, CancellationToken ct = default) { AddedEntities.Add(entity); return Task.CompletedTask; }
    public Task UpdateAsync(T entity, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
    public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
    public void Add(T entity) { AddedEntities.Add(entity); }
    public void Update(T entity) { }
    public void Remove(T entity) { }
}

internal sealed class FakePositionVacancyRepo(PositionVacancy? vacancy)
    : FakeNotificationRepoBase<PositionVacancy>, IPositionVacancyRepository
{
    public override Task<PositionVacancy?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => Task.FromResult(vacancy);
    public Task<List<PositionVacancy>> GetOpenAsync() => Task.FromResult(new List<PositionVacancy>());
    public Task<List<PositionVacancy>> GetOpenByRailroadAsync(ControlNumber r) => Task.FromResult(new List<PositionVacancy>());
    public Task<List<PositionVacancy>> GetByTargetAsync(string t, ControlNumber c) => Task.FromResult(new List<PositionVacancy>());
    public Task<List<PositionVacancy>> GetByCraftAsync(ControlNumber c) => Task.FromResult(new List<PositionVacancy>());
    public Task<List<PositionVacancy>> GetByWorkAreaAsync(ControlNumber w) => Task.FromResult(new List<PositionVacancy>());
    public Task<double> GetAverageDailyBoardVacanciesAsync(ControlNumber w, ControlNumber c, CancellationToken ct = default) => Task.FromResult(0.0);
}

internal sealed class FakeDynamicGroupRepo(DynamicGroup? workArea)
    : FakeNotificationRepoBase<DynamicGroup>, IDynamicGroupRepository
{
    public override Task<DynamicGroup?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => Task.FromResult(workArea);
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

internal sealed class FakeEmployeeNotificationRepo
    : FakeNotificationRepoBase<EmployeeNotification>, IEmployeeNotificationRepository
{
    public List<EmployeeNotification> Seeded { get; } = [];

    public override Task<EmployeeNotification?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => Task.FromResult(Seeded.SingleOrDefault(n => n.CtrlNbr == ctrlNbr));

    public Task<List<EmployeeNotification>> GetByEmployeeAsync(ControlNumber e, CancellationToken ct = default) =>
        Task.FromResult(Seeded.Where(n => n.EmployeeCtrlNbr == e)
            .OrderByDescending(n => n.CreatedAtUtc).ToList());

    public Task<List<EmployeeNotification>> GetUnacknowledgedByEmployeeAsync(ControlNumber e, CancellationToken ct = default) =>
        Task.FromResult(Seeded.Where(n => n.EmployeeCtrlNbr == e && n.RequiresAcknowledgement && !n.IsAcknowledged)
            .OrderByDescending(n => n.CreatedAtUtc).ToList());
}

internal sealed class FakeEmployeeRepo(Employee? employeeByUserId) : FakeNotificationRepoBase<Employee>, IEmployeeRepository
{
    public Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber) => Task.FromResult<Employee?>(null);
    public Task<Employee?> GetByUserIdAsync(string userId, CancellationToken ct = default) => Task.FromResult(employeeByUserId);
    public Task<List<Employee>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr) => Task.FromResult(new List<Employee>());
    public Task<List<Employee>> GetListByClientCtrlNbrAsync(ControlNumber clientCtrlNbr) => Task.FromResult(new List<Employee>());
    public Task<List<Employee>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs, CancellationToken ct = default) => Task.FromResult(new List<Employee>());
}

/// <summary>
/// Focused fake unit of work exposing only the repositories used by
/// <see cref="EmployeeNotificationService"/> and <see cref="NotificationQueryService"/>:
/// Employees, PositionVacancies, DynamicGroups, and EmployeeNotifications.
/// All other members throw to keep the surface intentional.
/// </summary>
internal sealed class FakeNotificationUoW(PositionVacancy? vacancy, DynamicGroup? workArea, Employee? employee = null) : IOrchestrationUnitOfWork
{
    public FakePositionVacancyRepo Vacancies { get; } = new(vacancy);
    public FakeDynamicGroupRepo Groups { get; } = new(workArea);
    public FakeEmployeeNotificationRepo Notifications { get; } = new();
    public FakeEmployeeRepo EmployeeRepo { get; } = new(employee);

    public string CorrelationId => "test";
    public string OrchestrationId => "test";

    public IPositionVacancyRepository PositionVacancies => Vacancies;
    public IDynamicGroupRepository DynamicGroups => Groups;
    public IEmployeeNotificationRepository EmployeeNotifications => Notifications;
    public IEmployeeRepository Employees => EmployeeRepo;

    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public void Dispose() { }

    // ── Unused members ────────────────────────────────────────────────────
    public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken ct = default) => Task.CompletedTask;
    public IEmailAddressRepository EmailAddresses => throw new NotImplementedException();
    public IParentRepository Parents => throw new NotImplementedException();
    public IUserParentAssignmentRepository UserParentAssignments => throw new NotImplementedException();
    public IInvitationRepository Invitations => throw new NotImplementedException();
    public IPayrollTierRepository PayrollTiers => throw new NotImplementedException();
    public IAddressTypeRepository AddressTypes => throw new NotImplementedException();
    public IPhoneNumberTypeRepository PhoneNumberTypes => throw new NotImplementedException();
    public IEmailAddressTypeRepository EmailAddressTypes => throw new NotImplementedException();
    public IEmploymentStatusRepository EmploymentStatuses => throw new NotImplementedException();
    public IEmploymentStatusHistoryRepository EmploymentStatusHistory => throw new NotImplementedException();
    public IEmployeePriorServiceCreditRepository EmployeePriorServiceCredits => throw new NotImplementedException();
    public ICraftRepository Crafts => throw new NotImplementedException();
    public IRosterRepository Rosters => throw new NotImplementedException();
    public ISeniorityRepository Seniority => throw new NotImplementedException();
    public ISeniorityStateRepository SeniorityStates => throw new NotImplementedException();
    public ISeniorityStateVacancyConfigRepository SeniorityStateVacancyConfigs => throw new NotImplementedException();
    public IPendingSeniorityStateChangeRepository PendingSeniorityStateChanges => throw new NotImplementedException();
    public IGroupTypeRepository GroupTypes => throw new NotImplementedException();
    public IGroupAttributeDefinitionRepository AttributeDefinitions => throw new NotImplementedException();
    public IGroupAttributeValueRepository AttributeValues => throw new NotImplementedException();
    public IStaffablePositionRepository StaffablePositions => throw new NotImplementedException();
    public IPositionAssignmentRepository PositionAssignments => throw new NotImplementedException();
    public IBoardCascadePolicyRepository BoardCascadePolicies => throw new NotImplementedException();
    public IRosterBoardRepository RosterBoards => throw new NotImplementedException();
    public IRequiredPositionsStrategyRepository RequiredPositionsStrategies => throw new NotImplementedException();
    public ICraftRequiredPositionsStrategyRepository CraftRequiredPositionsStrategies => throw new NotImplementedException();
    public ICrewRepository Crews => throw new NotImplementedException();
    public ICrewPositionRepository CrewPositions => throw new NotImplementedException();
    public ICrewIncumbencyRepository CrewIncumbencies => throw new NotImplementedException();
    public ICrewAssignmentRepository CrewAssignments => throw new NotImplementedException();
    public ICrewAttachmentInstanceRepository CrewAttachmentInstances => throw new NotImplementedException();
    public IAssignmentRepository Assignments => throw new NotImplementedException();
    public IAssignmentScheduleRepository AssignmentSchedules => throw new NotImplementedException();
    public IDepartmentRepository Departments => throw new NotImplementedException();
    public ICraftRoleRepository CraftRoles => throw new NotImplementedException();
    public ICraftRoleQualificationRepository CraftRoleQualifications => throw new NotImplementedException();
    public IWorkInstanceRepository WorkInstances => throw new NotImplementedException();
    public IPositionSlotRepository PositionSlots => throw new NotImplementedException();
    public ISlotRequirementRepository SlotRequirements => throw new NotImplementedException();
    public IShiftDefinitionRepository ShiftDefinitions => throw new NotImplementedException();
    public IShiftInstanceRepository ShiftInstances => throw new NotImplementedException();
    public IOnDutyRecordRepository OnDutyRecords => throw new NotImplementedException();
    public IOffDutyRecordRepository OffDutyRecords => throw new NotImplementedException();
    public ICraftOperationsPolicyRepository CraftOperationsPolicies => throw new NotImplementedException();
    public ICraftDisplacementPolicyRepository CraftDisplacementPolicies => throw new NotImplementedException();
    public IDisplacementCaseRepository DisplacementCases => throw new NotImplementedException();
    public IDisplacementClaimRepository DisplacementClaims => throw new NotImplementedException();
    public IBulletinPolicyRepository BulletinPolicies => throw new NotImplementedException();
    public ISeniorityMovePolicyRepository SeniorityMovePolicies => throw new NotImplementedException();
    public ISeniorityMoveRepository SeniorityMoves => throw new NotImplementedException();
    public IDispatchProjectionRepository DispatchProjections => throw new NotImplementedException();
    public IDispatchDecisionLogRepository DispatchDecisionLogs => throw new NotImplementedException();
    public IDispatchOverrideRepository DispatchOverrides => throw new NotImplementedException();
    public IEmployeeBookingRepository EmployeeBookings => throw new NotImplementedException();
    public IEmployeeCertificationRepository EmployeeCertifications => throw new NotImplementedException();
    public IEmployeeCertificationReadRepository EmployeeCertificationReads => throw new NotImplementedException();
    public IFraCertificationConfigRepository FraCertificationConfigs => throw new NotImplementedException();
    public IFraCertificationCheckConfigRepository FraCertificationCheckConfigs => throw new NotImplementedException();
    public IFraDutyTourRepository FraDutyTours => throw new NotImplementedException();
    public IRegulatoryStandardRepository RegulatoryStandards => throw new NotImplementedException();
    public IRegulatoryQualificationRepository RegulatoryQualifications => throw new NotImplementedException();
    public ICertificationRevocationRepository CertificationRevocations => throw new NotImplementedException();
    public IDrugAlcoholTestRepository DrugAlcoholTests => throw new NotImplementedException();
    public IDrugAlcoholActionRepository DrugAlcoholActions => throw new NotImplementedException();
    public IVoluntaryReferralRepository VoluntaryReferrals => throw new NotImplementedException();
    public IQualificationTypeRepository QualificationTypes => throw new NotImplementedException();
    public IQualificationRequirementRepository QualificationRequirements => throw new NotImplementedException();
    public IEmployeeQualificationRepository EmployeeQualifications => throw new NotImplementedException();
    public IEmployeeQualificationSuspensionRepository QualificationSuspensions => throw new NotImplementedException();
    public IAbsenceRequestRepository AbsenceRequests => throw new NotImplementedException();
    public IVacancyImpactRepository VacancyImpacts => throw new NotImplementedException();
    public ISafetyObservationRepository SafetyObservations => throw new NotImplementedException();
    public ISafetyObservationResolutionRepository SafetyResolutions => throw new NotImplementedException();
    public ISafetyCategoryRepository SafetyCategories => throw new NotImplementedException();
    public IRailroadInformationRepository RailroadInformation => throw new NotImplementedException();
    public IRailroadInformationReadReceiptRepository RailroadInformationReadReceipts => throw new NotImplementedException();
    public ITimeEntryRepository TimeEntries => throw new NotImplementedException();
    public IPayrollRunRepository PayrollRuns => throw new NotImplementedException();
    public IPayrollRecordRepository PayrollRecords => throw new NotImplementedException();
    public IPayrollExportBatchRepository PayrollExportBatches => throw new NotImplementedException();
    public IPayrollImportRecordRepository PayrollImportRecords => throw new NotImplementedException();
    public IHolidayRepository Holidays => throw new NotImplementedException();
    public IHolidayQualificationRuleRepository HolidayQualificationRules => throw new NotImplementedException();
    public IHolidayPayrollRecordRepository HolidayPayrollRecords => throw new NotImplementedException();
    public IEarningCodeRuleRepository EarningCodeRules => throw new NotImplementedException();
    public IPayRateRepository PayRates => throw new NotImplementedException();
    public IRailroadHolidaySelectionRepository RailroadHolidaySelections => throw new NotImplementedException();
    public IRoleRepository Roles => throw new NotImplementedException();
    public IFeatureRepository Features => throw new NotImplementedException();
    public IPermissionRepository Permissions => throw new NotImplementedException();
    public IBulletinRepository Bulletins => throw new NotImplementedException();
    public IBulletinBidRepository BulletinBids => throw new NotImplementedException();
    public IBulletinRuleRepository BulletinRules => throw new NotImplementedException();
}
