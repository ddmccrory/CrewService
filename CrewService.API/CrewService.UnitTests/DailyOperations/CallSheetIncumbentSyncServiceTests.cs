using CrewService.Application.DailyOperations;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Models.Parents;
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

namespace CrewService.UnitTests.DailyOperations;

public sealed class CallSheetIncumbentSyncServiceTests
{
    [Fact]
    public async Task SyncStaffablePositionIncumbentAsync_RemovesVacatedAndAddsAwardedOnDutyRecord()
    {
        var staffablePositionCtrlNbr = ControlNumber.Create(500);
        var craftRole = CraftRole.Create(ControlNumber.Create(30), "ENG", "Engineer");
        var crewPosition = CrewPosition.Create(ControlNumber.Create(10), craftRole.CtrlNbr, 1, staffablePositionCtrlNbr);

        var workInstance = WorkInstance.Create(
            assignmentGroupCtrlNbr: null,
            workAreaGroupCtrlNbr: ControlNumber.Create(1),
            startUtc: new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc),
            endUtc: new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc),
            callTimeUtc: null);

        var shift = ShiftInstance.Create(workInstance.CtrlNbr, ControlNumber.Create(1001), "1", "First Shift");
        var slot = shift.AddPositionSlot(
            crewPosition.CtrlNbr,
            ControlNumber.Create(111),
            1,
            ControlNumber.Create(2000),
            "ASG-1",
            "Assignment 1",
            "Engineer",
            "Group",
            "G1",
            new TimeOnly(7, 0),
            new TimeOnly(15, 0));

        var existingVacated = OnDutyRecord.CreateScheduled(
            slot.CtrlNbr,
            ControlNumber.Create(111),
            new DateTime(2026, 7, 17, 7, 0, 0, DateTimeKind.Utc),
            previousRestHours: 10,
            consecutiveDays: 1,
            isAssigned: true);

        var uow = new FakeUow(
            crewPosition,
            craftRole,
            craftPolicy: CraftCallSheetRule.Create(craftRole.CraftCtrlNbr, isEnabled: true, preOnDutyChangeCutoffMinutes: 0),
            shifts: [shift],
            workInstances: [workInstance],
            onDutySeed: [existingVacated]);

        await CallSheetIncumbentSyncService.SyncStaffablePositionIncumbentAsync(
            uow,
            staffablePositionCtrlNbr,
            ControlNumber.Create(222),
            TestContext.Current.CancellationToken);

        Assert.Contains(existingVacated, uow.OnDutyRepo.Removed);
        var added = Assert.Single(uow.OnDutyRepo.Added);
        Assert.Equal(ControlNumber.Create(222), added.EmployeeCtrlNbr);
        Assert.Equal(OnDutyStatus.Scheduled, added.Status);
        Assert.Equal(ControlNumber.Create(222), slot.IncumbentEmployeeCtrlNbr);
        Assert.True(slot.IsIncumbent);
        Assert.Single(uow.ShiftRepo.Updated);
    }

    [Fact]
    public async Task SyncStaffablePositionIncumbentAsync_UsesWorkAreaTimezoneForScheduledOnDutyUtc()
    {
        var nonUtcZone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(z => z.BaseUtcOffset != TimeSpan.Zero)
            ?? throw new InvalidOperationException("No non-UTC timezone available on this system.");

        var staffablePositionCtrlNbr = ControlNumber.Create(510);
        var craftRole = CraftRole.Create(ControlNumber.Create(35), "ENG", "Engineer");
        var crewPosition = CrewPosition.Create(ControlNumber.Create(20), craftRole.CtrlNbr, 1, staffablePositionCtrlNbr);
        var workArea = DynamicGroup.Create(
            groupTypeCtrlNbr: ControlNumber.Create(900),
            name: "Test Work Area",
            parentGroupCtrlNbr: null,
            path: "/700",
            isWorkArea: true,
            timeZoneId: nonUtcZone.Id);

        var workInstance = WorkInstance.Create(
            assignmentGroupCtrlNbr: null,
            workAreaGroupCtrlNbr: workArea.CtrlNbr,
            startUtc: new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            endUtc: new DateTime(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc),
            callTimeUtc: null);

        var shift = ShiftInstance.Create(workInstance.CtrlNbr, ControlNumber.Create(1010), "1", "First Shift");
        var slot = shift.AddPositionSlot(
            crewPosition.CtrlNbr,
            incumbentEmployeeCtrlNbr: null,
            displayOrder: 1,
            assignmentCtrlNbr: ControlNumber.Create(2500),
            assignmentCode: "ASG-TZ",
            assignmentName: "Timezone Assignment",
            craftRoleName: "Engineer",
            groupName: "Group",
            groupCode: "G1",
            onDutyTime: new TimeOnly(7, 0),
            offDutyTime: new TimeOnly(15, 0));

        var uow = new FakeUow(
            crewPosition,
            craftRole,
            craftPolicy: CraftCallSheetRule.Create(craftRole.CraftCtrlNbr, isEnabled: true, preOnDutyChangeCutoffMinutes: 0),
            shifts: [shift],
            workInstances: [workInstance],
            onDutySeed: [],
            dynamicGroup: workArea);

        await CallSheetIncumbentSyncService.SyncStaffablePositionIncumbentAsync(
            uow,
            staffablePositionCtrlNbr,
            ControlNumber.Create(999),
            TestContext.Current.CancellationToken);

        var added = Assert.Single(uow.OnDutyRepo.Added);
        var expectedOnDutyUtc = TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 7, 20, 7, 0, 0, DateTimeKind.Unspecified), nonUtcZone);

        Assert.Equal(expectedOnDutyUtc, added.ScheduledOnDutyTimeUtc);
        Assert.Equal(ControlNumber.Create(999), slot.IncumbentEmployeeCtrlNbr);
    }

    [Fact]
    public async Task SyncStaffablePositionIncumbentAsync_WhenInsideCutoff_DoesNotChangeIncumbent()
    {
        var staffablePositionCtrlNbr = ControlNumber.Create(501);
        var craftRole = CraftRole.Create(ControlNumber.Create(31), "ENG", "Engineer");
        var crewPosition = CrewPosition.Create(ControlNumber.Create(11), craftRole.CtrlNbr, 1, staffablePositionCtrlNbr);

        var todayStart = DateTime.UtcNow.Date;
        var workInstance = WorkInstance.Create(
            assignmentGroupCtrlNbr: null,
            workAreaGroupCtrlNbr: ControlNumber.Create(1),
            startUtc: todayStart,
            endUtc: todayStart.AddDays(1),
            callTimeUtc: null);

        var shift = ShiftInstance.Create(workInstance.CtrlNbr, ControlNumber.Create(1003), "1", "First Shift");
        var slot = shift.AddPositionSlot(
            crewPosition.CtrlNbr,
            ControlNumber.Create(333),
            1,
            ControlNumber.Create(2001),
            "ASG-2",
            "Assignment 2",
            "Engineer",
            "Group",
            "G1",
            TimeOnly.FromDateTime(DateTime.UtcNow.AddMinutes(30)),
            TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(8)));

        var uow = new FakeUow(
            crewPosition,
            craftRole,
            craftPolicy: CraftCallSheetRule.Create(craftRole.CraftCtrlNbr, isEnabled: true, preOnDutyChangeCutoffMinutes: 120),
            shifts: [shift],
            workInstances: [workInstance],
            onDutySeed: []);

        await CallSheetIncumbentSyncService.SyncStaffablePositionIncumbentAsync(
            uow,
            staffablePositionCtrlNbr,
            ControlNumber.Create(444),
            TestContext.Current.CancellationToken);

        Assert.Equal(ControlNumber.Create(333), slot.IncumbentEmployeeCtrlNbr);
        Assert.Empty(uow.OnDutyRepo.Added);
        Assert.Empty(uow.OnDutyRepo.Removed);
        Assert.Empty(uow.ShiftRepo.Updated);
    }

    [Fact]
    public async Task SyncStaffablePositionIncumbentAsync_WhenWorkInstanceMissing_Throws()
    {
        var staffablePositionCtrlNbr = ControlNumber.Create(502);
        var craftRole = CraftRole.Create(ControlNumber.Create(32), "ENG", "Engineer");
        var crewPosition = CrewPosition.Create(ControlNumber.Create(12), craftRole.CtrlNbr, 1, staffablePositionCtrlNbr);

        var shift = ShiftInstance.Create(ControlNumber.Create(123456), ControlNumber.Create(1004), "1", "First Shift");
        shift.AddPositionSlot(
            crewPosition.CtrlNbr,
            ControlNumber.Create(555),
            1,
            ControlNumber.Create(2002),
            "ASG-3",
            "Assignment 3",
            "Engineer",
            "Group",
            "G1",
            new TimeOnly(7, 0),
            new TimeOnly(15, 0));

        var uow = new FakeUow(
            crewPosition,
            craftRole,
            craftPolicy: null,
            shifts: [shift],
            workInstances: [],
            onDutySeed: []);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CallSheetIncumbentSyncService.SyncStaffablePositionIncumbentAsync(
                uow,
                staffablePositionCtrlNbr,
                ControlNumber.Create(666),
                TestContext.Current.CancellationToken));

        Assert.Contains("Work instance", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeUow(
        CrewPosition crewPosition,
        CraftRole craftRole,
        CraftCallSheetRule? craftPolicy,
        IReadOnlyList<ShiftInstance> shifts,
        IReadOnlyList<WorkInstance> workInstances,
        IReadOnlyList<OnDutyRecord> onDutySeed,
        DynamicGroup? dynamicGroup = null) : IOrchestrationUnitOfWork
    {
        public string CorrelationId => "test";
        public string OrchestrationId => "test";

        public FakeCrewPositionRepo CrewPositionRepo { get; } = new(crewPosition);
        public FakeCraftRoleRepo CraftRoleRepo { get; } = new(craftRole);
        public FakeCraftCallSheetRuleRepo CraftRuleRepo { get; } = new(craftPolicy);
        public FakeShiftInstanceRepo ShiftRepo { get; } = new(shifts);
        public FakeWorkInstanceRepo WorkInstanceRepo { get; } = new(workInstances);
        public FakeOnDutyRepo OnDutyRepo { get; } = new(onDutySeed);
        public FakeOffDutyRepo OffDutyRepo { get; } = new();
        public FakeDynamicGroupRepo DynamicGroupRepo { get; } = new(dynamicGroup);

        public ICrewPositionRepository CrewPositions => CrewPositionRepo;
        public ICraftRoleRepository CraftRoles => CraftRoleRepo;
        public ICraftCallSheetRuleRepository CraftCallSheetRules => CraftRuleRepo;
        public IShiftInstanceRepository ShiftInstances => ShiftRepo;
        public IWorkInstanceRepository WorkInstances => WorkInstanceRepo;
        public IOnDutyRecordRepository OnDutyRecords => OnDutyRepo;
        public IOffDutyRecordRepository OffDutyRecords => OffDutyRepo;

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
        public ICraftRepository Crafts => null!;
        public IRosterRepository Rosters => null!;
        public ISeniorityRepository Seniority => null!;
        public ISeniorityStateRepository SeniorityStates => null!;
        public ISeniorityStateVacancyConfigRepository SeniorityStateVacancyConfigs => null!;
        public IPendingSeniorityStateChangeRepository PendingSeniorityStateChanges => null!;
        public IGroupTypeRepository GroupTypes => null!;
        public IDynamicGroupRepository DynamicGroups => DynamicGroupRepo;
        public IGroupAttributeDefinitionRepository AttributeDefinitions => null!;
        public IGroupAttributeValueRepository AttributeValues => null!;
        public IStaffablePositionRepository StaffablePositions => null!;
        public IPositionAssignmentRepository PositionAssignments => null!;
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
        public IDepartmentRepository Departments => null!;
        public ICraftRoleQualificationRepository CraftRoleQualifications => null!;
        public IPositionSlotRepository PositionSlots => null!;
        public ISlotRequirementRepository SlotRequirements => null!;
        public IShiftDefinitionRepository ShiftDefinitions => null!;
        public ICraftOperationsPolicyRepository CraftOperationsPolicies => null!;
        public ICraftDisplacementPolicyRepository CraftDisplacementPolicies => null!;
        public IDisplacementCaseRepository DisplacementCases => null!;
        public IDisplacementClaimRepository DisplacementClaims => null!;
        public IBulletinPolicyRepository BulletinPolicies => null!;
        public IAbsenceApprovalPolicyRepository AbsenceApprovalPolicies => null!;
        public ICallSheetRuleRepository CallSheetRules => null!;
        public IDepartmentReassignmentRuleRepository DepartmentReassignmentRules => null!;
        public ISeniorityMovePolicyRepository SeniorityMovePolicies => null!;
        public ISeniorityMoveRepository SeniorityMoves => null!;
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
        public IEmployeeNotificationRepository EmployeeNotifications => null!;
        public INotificationTypeConfigRepository NotificationTypeConfigs => null!;
    }

    private abstract class RepoBase<TEntity> : IRepository<TEntity> where TEntity : Entity
    {
        public virtual Task<List<TEntity>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<TEntity>());
        public virtual Task<List<TEntity>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<TEntity>());
        public virtual Task<TEntity?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<TEntity?>(default);
        public virtual Task<TEntity?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<TEntity?>(default);
        public virtual Task AddAsync(TEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task UpdateAsync(TEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public virtual void Add(TEntity entity) { }
        public virtual void Update(TEntity entity) { }
        public virtual void Remove(TEntity entity) { }
    }

    private sealed class FakeCrewPositionRepo(CrewPosition crewPosition) : RepoBase<CrewPosition>, ICrewPositionRepository
    {
        public Task<List<CrewPosition>> GetByCrewAsync(ControlNumber crewCtrlNbr) => Task.FromResult(new List<CrewPosition>());
        public Task<List<CrewPosition>> GetByCrewsAsync(IEnumerable<ControlNumber> crewCtrlNbrs) => Task.FromResult(new List<CrewPosition>());
        public Task<CrewPosition?> GetByStaffablePositionAsync(ControlNumber staffablePositionCtrlNbr)
            => Task.FromResult<CrewPosition?>(crewPosition.StaffablePositionCtrlNbr == staffablePositionCtrlNbr ? crewPosition : null);
        public Task<List<ControlNumber>> GetVacantStaffablePositionCtrlNbrsAsync(CancellationToken ct = default)
            => Task.FromResult(new List<ControlNumber>());
    }

    private sealed class FakeCraftRoleRepo(CraftRole craftRole) : RepoBase<CraftRole>, ICraftRoleRepository
    {
        public override Task<CraftRole?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<CraftRole?>(craftRole.CtrlNbr == ctrlNbr ? craftRole : null);

        public Task<List<CraftRole>> GetByCraftAsync(ControlNumber craftCtrlNbr) => Task.FromResult(new List<CraftRole>());
        public Task<List<CraftRole>> GetByDepartmentAsync(ControlNumber departmentCtrlNbr) => Task.FromResult(new List<CraftRole>());
        public Task<List<CraftRole>> GetByRailroadAsync(ControlNumber railroadCtrlNbr) => Task.FromResult(new List<CraftRole>());
        public Task<CraftRole?> GetByCtrlNbrWithQualificationsAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<CraftRole?>(null);
    }

    private sealed class FakeCraftCallSheetRuleRepo(CraftCallSheetRule? rule) : RepoBase<CraftCallSheetRule>, ICraftCallSheetRuleRepository
    {
        public Task<CraftCallSheetRule?> GetByCraftAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult(rule?.CraftCtrlNbr == craftCtrlNbr ? rule : null);

        public Task<List<CraftCallSheetRule>> GetByCraftsAsync(IEnumerable<ControlNumber> craftCtrlNbrs)
            => Task.FromResult(new List<CraftCallSheetRule>());
    }

    private sealed class FakeDynamicGroupRepo(DynamicGroup? group) : RepoBase<DynamicGroup>, IDynamicGroupRepository
    {
        public override Task<DynamicGroup?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(group?.CtrlNbr == ctrlNbr ? group : null);

        public Task<List<DynamicGroup>> GetByParentCtrlNbrAsync(ControlNumber? parentGroupCtrlNbr)
            => Task.FromResult(new List<DynamicGroup>());

        public Task<List<DynamicGroup>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs)
            => Task.FromResult(new List<DynamicGroup>());

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

    private sealed class FakeShiftInstanceRepo(IReadOnlyList<ShiftInstance> shifts) : RepoBase<ShiftInstance>, IShiftInstanceRepository
    {
        public List<ShiftInstance> Updated { get; } = [];

        public Task<IReadOnlyList<ShiftInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShiftInstance>>([]);

        public Task<bool> ExistsByWorkInstanceAndShiftCodeAsync(ControlNumber workInstanceCtrlNbr, string shiftCode, ControlNumber? departmentCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<ShiftInstance>> GetIncompleteByCrewPositionAsync(ControlNumber crewPositionCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShiftInstance>>(shifts.Where(s => s.PositionSlots.Any(p => p.CrewPositionCtrlNbr == crewPositionCtrlNbr)).ToList());

        public override Task UpdateAsync(ShiftInstance entity, CancellationToken ct = default)
        {
            Updated.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWorkInstanceRepo(IReadOnlyList<WorkInstance> workInstances) : RepoBase<WorkInstance>, IWorkInstanceRepository
    {
        public override Task<WorkInstance?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(workInstances.FirstOrDefault(w => w.CtrlNbr == ctrlNbr));

        public Task<List<WorkInstance>> GetByWorkAreaAndDateRangeAsync(ControlNumber workAreaGroupCtrlNbr, DateTime startUtc, DateTime endUtc)
            => Task.FromResult(new List<WorkInstance>());
    }

    private sealed class FakeOnDutyRepo(IReadOnlyList<OnDutyRecord> seed) : RepoBase<OnDutyRecord>, IOnDutyRecordRepository
    {
        private readonly List<OnDutyRecord> _records = [.. seed];
        public List<OnDutyRecord> Added { get; } = [];
        public List<OnDutyRecord> Removed { get; } = [];

        public Task<IReadOnlyList<OnDutyRecord>> GetRecentForEmployeeAsync(ControlNumber employeeCtrlNbr, int dayCount, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>([]);

        public Task<IReadOnlyList<OnDutyRecord>> GetByPositionSlotsAsync(IReadOnlyList<ControlNumber> positionSlotCtrlNbrs, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>(_records.Where(r => positionSlotCtrlNbrs.Contains(r.PositionSlotCtrlNbr)).ToList());

        public Task<IReadOnlyList<OnDutyRecord>> GetOpenForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>([]);

        public Task<IReadOnlyList<OnDutyRecord>> GetIncompleteForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>([]);

        public Task<IReadOnlyList<OnDutyRecord>> GetNotStartedForRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>([]);

        public Task<IReadOnlyList<OnDutyRecord>> GetForEmployeeInRangeAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>([]);

        public Task<IReadOnlyList<OnDutyCompletionStatus>> GetCompletionStatusesForShiftAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyCompletionStatus>>([]);

        public Task<OnDutyTieUpContext?> GetTieUpContextAsync(ControlNumber onDutyRecordCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<OnDutyTieUpContext?>(null);

        public override Task AddAsync(OnDutyRecord entity, CancellationToken ct = default)
        {
            Added.Add(entity);
            _records.Add(entity);
            return Task.CompletedTask;
        }

        public override void Remove(OnDutyRecord entity)
        {
            Removed.Add(entity);
            _records.Remove(entity);
        }
    }

    private sealed class FakeOffDutyRepo : RepoBase<OffDutyRecord>, IOffDutyRecordRepository
    {
        public Task<OffDutyRecord?> GetLastForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<OffDutyRecord?>(null);

        public Task<IReadOnlyList<OffDutyRecord>> GetByOnDutyRecordsAsync(IReadOnlyList<ControlNumber> onDutyRecordCtrlNbrs, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OffDutyRecord>>([]);
    }
}