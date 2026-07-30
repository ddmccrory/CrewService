using CrewService.Application.DailyOperations;
using CrewService.Application.Absence;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
using CrewService.Application.VacancyAssignment;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Models.Seniority;
using Xunit;

namespace CrewService.UnitTests.DailyOperations;

public class CallSheetGenerationServiceTests
{
    private sealed class FakeAssignmentQueryService(IReadOnlyList<AssignmentDto> templates) : IAssignmentQueryService
    {
        public Task<IReadOnlyList<AssignmentDto>> GetTemplatesForDateAsync(
            ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftDefinitionCtrlNbr,
            DateOnly targetDate, ControlNumber? departmentCtrlNbr = null, CancellationToken ct = default)
            => Task.FromResult(templates);

        public Task<IReadOnlyList<AssignmentDto>> GetExtraAssignmentsForShiftAsync(
            ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftDefinitionCtrlNbr,
            DateOnly targetDate, ControlNumber? departmentCtrlNbr = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AssignmentDto>>([]);
    }

    private sealed class FakeShiftDefinitionRepository(ShiftDefinition? shiftDef) : FakeRepository<ShiftDefinition>, IShiftDefinitionRepository
    {
        public override Task<ShiftDefinition?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(shiftDef);

        public Task<List<ShiftDefinition>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr)
            => Task.FromResult(new List<ShiftDefinition>());
    }

    private sealed class FakeShiftInstanceRepository : IShiftInstanceRepository
    {
        public readonly List<ShiftInstance> Added = [];

        public Task AddAsync(ShiftInstance instance, CancellationToken ct = default)
        {
            Added.Add(instance);
            return Task.CompletedTask;
        }

        public Task<ShiftInstance?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<ShiftInstance?>(null);

        public Task<IReadOnlyList<ShiftInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShiftInstance>>([]);

        public Task<bool> ExistsByWorkInstanceAndShiftCodeAsync(ControlNumber workInstanceCtrlNbr, string shiftCode, ControlNumber? departmentCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<ShiftInstance>> GetIncompleteByCrewPositionAsync(ControlNumber crewPositionCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShiftInstance>>([]);

        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task UpdateAsync(ShiftInstance instance, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<List<ShiftInstance>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ShiftInstance>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ShiftInstance?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => throw new NotImplementedException();
        public void Add(ShiftInstance entity) => throw new NotImplementedException();
        public void Update(ShiftInstance entity) => throw new NotImplementedException();
        public void Remove(ShiftInstance entity) => throw new NotImplementedException();
    }

    private sealed class FakeWorkInstanceRepository : FakeRepository<WorkInstance>, IWorkInstanceRepository
    {
        public readonly List<WorkInstance> Added = [];

        public override Task AddAsync(WorkInstance entity, CancellationToken ct = default)
        {
            Added.Add(entity);
            return Task.CompletedTask;
        }

        public override Task<WorkInstance?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<WorkInstance?>(Added.FirstOrDefault(w => w.CtrlNbr == ctrlNbr));

        public Task<List<WorkInstance>> GetByWorkAreaAndDateRangeAsync(
            ControlNumber workAreaGroupCtrlNbr, DateTime startUtc, DateTime endUtc)
            => Task.FromResult(Added
                .Where(w => w.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && w.StartUtc >= startUtc && w.EndUtc <= endUtc)
                .ToList());
    }

    private sealed class FakeDepartmentRepository : FakeRepository<Department>, IDepartmentRepository
    {
        public Task<List<Department>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr)
            => Task.FromResult(new List<Department>());
    }

    private sealed class FakeDynamicGroupRepository : FakeRepository<DynamicGroup>, IDynamicGroupRepository
    {
        public Task<List<DynamicGroup>> GetByParentCtrlNbrAsync(ControlNumber? parentGroupCtrlNbr) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs) => Task.FromResult(new List<DynamicGroup>());
        public Task<DynamicGroup?> GetByGroupTypeAndNameIncludingDeletedAsync(ControlNumber groupTypeCtrlNbr, string name) => Task.FromResult<DynamicGroup?>(null);
        public Task<List<DynamicGroup>> GetWorkAreasAsync(ControlNumber? railroadCtrlNbr = null) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetWorkAreasWithDescendantsAsync() => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetAncestorsAsync(ControlNumber groupCtrlNbr) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetTreeAsync(ControlNumber? rootCtrlNbr = null) => Task.FromResult(new List<DynamicGroup>());
        public Task<List<DynamicGroup>> GetByGroupTypeNameAsync(string typeName, ControlNumber? parentCtrlNbr = null) => Task.FromResult(new List<DynamicGroup>());
        public Task BackfillPathsAsync() => Task.CompletedTask;
    }

    private sealed class FakeOnDutyRecordRepository : FakeRepository<OnDutyRecord>, IOnDutyRecordRepository
    {
        public readonly List<OnDutyRecord> Added = [];
        public readonly List<OnDutyRecord> Removed = [];

        public override Task AddAsync(OnDutyRecord entity, CancellationToken ct = default)
        {
            Added.Add(entity);
            return Task.CompletedTask;
        }

        public override void Remove(OnDutyRecord entity) => Removed.Add(entity);

        public Task<IReadOnlyList<OnDutyRecord>> GetRecentForEmployeeAsync(ControlNumber employeeCtrlNbr, int dayCount, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>([]);

        public Task<IReadOnlyList<OnDutyRecord>> GetByPositionSlotsAsync(IReadOnlyList<ControlNumber> positionSlotCtrlNbrs, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OnDutyRecord>>(
                [.. Added.Where(r => positionSlotCtrlNbrs.Contains(r.PositionSlotCtrlNbr))]);

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
    }

    private sealed class FakeOffDutyRecordRepository : FakeRepository<OffDutyRecord>, IOffDutyRecordRepository
    {
        public Task<OffDutyRecord?> GetLastForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<OffDutyRecord?>(null);

        public Task<IReadOnlyList<OffDutyRecord>> GetByOnDutyRecordsAsync(IReadOnlyList<ControlNumber> onDutyRecordCtrlNbrs, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OffDutyRecord>>([]);
    }

    private sealed class FakeCrewPositionRepository(IReadOnlyDictionary<ControlNumber, CrewPosition>? byCtrlNbr = null)
        : FakeRepository<CrewPosition>, ICrewPositionRepository
    {
        private readonly IReadOnlyDictionary<ControlNumber, CrewPosition> _byCtrlNbr =
            byCtrlNbr ?? new Dictionary<ControlNumber, CrewPosition>();

        public override Task<CrewPosition?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult(_byCtrlNbr.GetValueOrDefault(ctrlNbr));

        public Task<List<CrewPosition>> GetByCrewAsync(ControlNumber crewCtrlNbr) => Task.FromResult(new List<CrewPosition>());
        public Task<List<CrewPosition>> GetByCrewsAsync(IEnumerable<ControlNumber> crewCtrlNbrs) => Task.FromResult(new List<CrewPosition>());
        public Task<CrewPosition?> GetByStaffablePositionAsync(ControlNumber staffablePositionCtrlNbr) => Task.FromResult<CrewPosition?>(null);
        public Task<List<ControlNumber>> GetVacantStaffablePositionCtrlNbrsAsync(CancellationToken ct = default) => Task.FromResult(new List<ControlNumber>());
    }

    private sealed class FakePositionAssignmentRepository(IReadOnlyList<PositionAssignment>? assignments = null)
        : FakeRepository<PositionAssignment>, IPositionAssignmentRepository
    {
        private readonly IReadOnlyList<PositionAssignment> _assignments = assignments ?? [];

        public Task<PositionAssignment?> GetByStaffablePositionAsync(ControlNumber staffablePositionCtrlNbr)
            => Task.FromResult<PositionAssignment?>(null);
        public Task<List<PositionAssignment>> GetByStaffablePositionsAsync(IEnumerable<ControlNumber> staffablePositionCtrlNbrs)
            => Task.FromResult(new List<PositionAssignment>());
        public Task<List<PositionAssignment>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr)
            => Task.FromResult(_assignments.Where(a => a.EmployeeCtrlNbr == employeeCtrlNbr).ToList());
        public Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsAsync() => Task.FromResult(new HashSet<long>());
        public Task<HashSet<long>> GetAssignedEmployeeCtrlNbrsByTypeAsync(string assignmentType) => Task.FromResult(new HashSet<long>());
    }

    /// <summary>
    /// Minimal fake for <see cref="IRepository{TEntity}"/>. Override specific members as needed.
    /// </summary>
    private abstract class FakeRepository<TEntity> : IRepository<TEntity> where TEntity : Entity
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

    private sealed class FakeCallSheetUoW(
        IShiftDefinitionRepository shiftDefinitions,
        IShiftInstanceRepository shiftInstances,
        IWorkInstanceRepository workInstances,
        IDepartmentRepository departments,
        IDynamicGroupRepository dynamicGroups,
        IOnDutyRecordRepository onDutyRecords,
        IOffDutyRecordRepository offDutyRecords,
        ICrewPositionRepository crewPositions,
        IPositionAssignmentRepository positionAssignments) : IOrchestrationUnitOfWork
    {
        public string CorrelationId => string.Empty;
        public string OrchestrationId => string.Empty;
        public IShiftDefinitionRepository ShiftDefinitions => shiftDefinitions;
        public IShiftInstanceRepository ShiftInstances => shiftInstances;
        public IWorkInstanceRepository WorkInstances => workInstances;
        public IDepartmentRepository Departments => departments;
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
                public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }

        // Remaining members not used by CallSheetGenerationService
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
        public IDynamicGroupRepository DynamicGroups => dynamicGroups;
        public IGroupAttributeDefinitionRepository AttributeDefinitions => null!;
        public IGroupAttributeValueRepository AttributeValues => null!;
        public IStaffablePositionRepository StaffablePositions => null!;
        public IPositionAssignmentRepository PositionAssignments => positionAssignments;
        public IBoardCascadePolicyRepository BoardCascadePolicies => null!;
        public IRequiredPositionsStrategyRepository RequiredPositionsStrategies => throw new NotImplementedException();
        public ICraftRequiredPositionsStrategyRepository CraftRequiredPositionsStrategies => throw new NotImplementedException();
        public IRosterBoardRepository RosterBoards => null!;
        public ICrewRepository Crews => null!;
        public ICrewPositionRepository CrewPositions => crewPositions;
        public ICrewIncumbencyRepository CrewIncumbencies => null!;
        public ICrewAssignmentRepository CrewAssignments => null!;
        public ICrewAttachmentInstanceRepository CrewAttachmentInstances => null!;
        public IAssignmentRepository Assignments => null!;
        public IAssignmentScheduleRepository AssignmentSchedules => null!;
        public ICraftRoleRepository CraftRoles => null!;
        public ICraftRoleQualificationRepository CraftRoleQualifications => null!;
        public IPositionSlotRepository PositionSlots => null!;
        public ISlotRequirementRepository SlotRequirements => null!;
        public IOnDutyRecordRepository OnDutyRecords => onDutyRecords;
        public IOffDutyRecordRepository OffDutyRecords => offDutyRecords;
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
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IUserParentAssignmentRepository UserParentAssignments => null!;
        public IInvitationRepository Invitations => null!;
        public IPayrollTierRepository PayrollTiers => null!;
        public ISeniorityStateVacancyConfigRepository    SeniorityStateVacancyConfigs => throw new NotImplementedException();
        public IPendingSeniorityStateChangeRepository    PendingSeniorityStateChanges => throw new NotImplementedException();
    }

    private sealed class FakeCallSheetUoWFactory(
        IShiftDefinitionRepository shiftDefinitions,
        IShiftInstanceRepository shiftInstances,
        IWorkInstanceRepository workInstances,
        IDepartmentRepository departments,
        IDynamicGroupRepository dynamicGroups,
        IOnDutyRecordRepository onDutyRecords,
        IOffDutyRecordRepository offDutyRecords,
        ICrewPositionRepository crewPositions,
        IPositionAssignmentRepository positionAssignments) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(
            OrchestrationUnitOfWorkOptions? options = null, CancellationToken ct = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(
                new FakeCallSheetUoW(shiftDefinitions, shiftInstances, workInstances, departments,
                    dynamicGroups, onDutyRecords, offDutyRecords, crewPositions, positionAssignments));
    }

    private static WorkAreaClock CreateClock()
        => new(TimeProvider.System, null!);

    private static CallSheetGenerationService CreateSut(
        IAssignmentQueryService assignmentQuery,
        IShiftDefinitionRepository shiftDefinitions,
        IShiftInstanceRepository shiftInstances,
        IWorkInstanceRepository workInstances,
        IDepartmentRepository departments,
        IOnDutyRecordRepository? onDutyRecords = null,
        IOffDutyRecordRepository? offDutyRecords = null,
        IDynamicGroupRepository? dynamicGroups = null,
        ICrewPositionRepository? crewPositions = null,
        IPositionAssignmentRepository? positionAssignments = null)
    {
        var clock = CreateClock();
        var vacancyEvaluationService = new CallSheetSlotVacancyEvaluationService(
            clock,
            new NullRailroadResolver(),
            new NullAbsenceCodeRepository());
        var vacancyProjectionSyncService = new CallSheetVacancyProjectionSyncService(
            vacancyEvaluationService,
            new VacancyProjectionOrchestratorService(
                new EmptyBoardCandidateProvider(),
                new AlwaysRestedSkipContextProvider()));

        return new CallSheetGenerationService(
            new FakeCallSheetUoWFactory(
                shiftDefinitions, shiftInstances, workInstances, departments,
                dynamicGroups ?? new FakeDynamicGroupRepository(),
                onDutyRecords ?? new FakeOnDutyRecordRepository(),
                offDutyRecords ?? new FakeOffDutyRecordRepository(),
                crewPositions ?? new FakeCrewPositionRepository(),
                positionAssignments ?? new FakePositionAssignmentRepository()),
            assignmentQuery,
            clock,
            vacancyProjectionSyncService);
    }

    private sealed class EmptyBoardCandidateProvider : IBoardCandidateProvider
    {
        public Task<IReadOnlyList<SkipRuleCandidate>> GetCandidatesAsync(
            ControlNumber workAreaGroupCtrlNbr,
            ControlNumber craftCtrlNbr,
            SkipRuleSlot slot,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SkipRuleCandidate>>([]);
    }

    private sealed class AlwaysRestedSkipContextProvider : ISkipContextProvider
    {
        public Task<SkipContext> BuildAsync(SkipRuleCandidate candidate, SkipRuleSlot slot, CancellationToken ct = default)
            => Task.FromResult(new SkipContext { IsRested = true, IsQualified = true });
    }

    private sealed class NullRailroadResolver : IRailroadResolver
    {
        public Task<ControlNumber?> ResolveFromWorkAreaAsync(IOrchestrationUnitOfWork uow, ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<ControlNumber?>(null);

        public ControlNumber? ResolveFromGroup(DynamicGroup? group) => null;
    }

    private sealed class NullAbsenceCodeRepository : IAbsenceCodeRepository
    {
        public Task<List<AbsenceCode>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default) => Task.FromResult(new List<AbsenceCode>());
        public Task<AbsenceCodeCraftOverride?> GetOverrideAsync(ControlNumber absenceCodeCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceCodeCraftOverride?>(null);
        public Task<List<AbsenceCode>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<AbsenceCode>());
        public Task<List<AbsenceCode>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<AbsenceCode>());
        public Task<AbsenceCode?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceCode?>(null);
        public Task<AbsenceCode?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<AbsenceCode?>(null);
        public Task AddAsync(AbsenceCode entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(AbsenceCode entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(AbsenceCode entity) { }
        public void Update(AbsenceCode entity) { }
        public void Remove(AbsenceCode entity) { }
    }

    private static ShiftDefinition CreateActiveShiftDef(long workAreaCtrlNbr = 1)
    {
        return ShiftDefinition.Create(
            ControlNumber.Create(workAreaCtrlNbr),
            "1", "First Shift", 1, isActive: true);
    }

    [Fact]
    public async Task GenerateForShift_NoTemplatesOnDate_CreatesEmptyShiftInstance()
    {
        // Arrange - Saturday, no assignments scheduled
        var shiftDef = CreateActiveShiftDef();
        var shiftInstanceRepo = new FakeShiftInstanceRepository();

        var sut = CreateSut(
            new FakeAssignmentQueryService([]),
            new FakeShiftDefinitionRepository(shiftDef),
            shiftInstanceRepo,
            new FakeWorkInstanceRepository(),
            new FakeDepartmentRepository());

        // Act - should NOT throw
        var result = await sut.GenerateForShiftAsync(
            ControlNumber.Create(1),
            shiftDef.CtrlNbr,
            new DateOnly(2026, 4, 4),  // Saturday
            ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.ShiftCode);
        Assert.Empty(result.PositionSlots);
        Assert.Single(shiftInstanceRepo.Added);
    }

    [Fact]
    public async Task GenerateForShift_WithTemplates_CreatesPositionSlots()
    {
        // Arrange - weekday with one assignment having two positions
        var shiftDef = CreateActiveShiftDef();
        var shiftInstanceRepo = new FakeShiftInstanceRepository();

        var templates = new List<AssignmentDto>
        {
            new(ControlNumber.Create(130), ControlNumber.Create(1), null,
                "TY-101", "Pool Turn 101",
                new TimeOnly(7, 0), new TimeOnly(15, 0),
                "Test Group", "TG",
                [
                    new CrewPositionDto(ControlNumber.Create(10), ControlNumber.Create(200), 1, "Engineer"),
                    new CrewPositionDto(ControlNumber.Create(11), null, 2, "Conductor")
                ])
        };

        var sut = CreateSut(
            new FakeAssignmentQueryService(templates),
            new FakeShiftDefinitionRepository(shiftDef),
            shiftInstanceRepo,
            new FakeWorkInstanceRepository(),
            new FakeDepartmentRepository());

        // Act
        var result = await sut.GenerateForShiftAsync(
            ControlNumber.Create(1),
            shiftDef.CtrlNbr,
            new DateOnly(2026, 4, 6),  // Monday
            ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.PositionSlots.Count);
        Assert.Single(shiftInstanceRepo.Added);
    }

    [Fact]
    public async Task GenerateForShift_InactiveShift_Throws()
    {
        var shiftDef = ShiftDefinition.Create(
            ControlNumber.Create(1), "1", "First Shift", 1, isActive: false);

        var sut = CreateSut(
            new FakeAssignmentQueryService([]),
            new FakeShiftDefinitionRepository(shiftDef),
            new FakeShiftInstanceRepository(),
            new FakeWorkInstanceRepository(),
            new FakeDepartmentRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GenerateForShiftAsync(
                ControlNumber.Create(1), shiftDef.CtrlNbr,
                new DateOnly(2026, 4, 6),
                ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GenerateForShift_IncumbentSlot_CreatesScheduledOnDutyRecord()
    {
        // Arrange - one incumbent (Engineer) and one vacant (Conductor) position
        var shiftDef = CreateActiveShiftDef();
        var onDutyRepo = new FakeOnDutyRecordRepository();

        var templates = new List<AssignmentDto>
        {
            new(ControlNumber.Create(130), ControlNumber.Create(1), null,
                "TY-101", "Pool Turn 101",
                new TimeOnly(7, 0), new TimeOnly(15, 0),
                "Test Group", "TG",
                [
                    new CrewPositionDto(ControlNumber.Create(10), ControlNumber.Create(200), 1, "Engineer"),
                    new CrewPositionDto(ControlNumber.Create(11), null, 2, "Conductor")
                ])
        };

        var sut = CreateSut(
            new FakeAssignmentQueryService(templates),
            new FakeShiftDefinitionRepository(shiftDef),
            new FakeShiftInstanceRepository(),
            new FakeWorkInstanceRepository(),
            new FakeDepartmentRepository(),
            onDutyRecords: onDutyRepo);

        // Act
        await sut.GenerateForShiftAsync(
            ControlNumber.Create(1),
            shiftDef.CtrlNbr,
            new DateOnly(2026, 4, 6),  // Monday
            ct: TestContext.Current.CancellationToken);

        // Assert - exactly one record, for the incumbent, in Scheduled state, no late call
        var record = Assert.Single(onDutyRepo.Added);
        Assert.Equal(200, record.EmployeeCtrlNbr.Value);
        Assert.Equal(OnDutyStatus.Scheduled, record.Status);
        Assert.False(record.IsLateCall);
        Assert.False(record.IsAssigned);
        Assert.Equal(record.ScheduledOnDutyTimeUtc, record.OnDutyTimeUtc);
        // No work-area timezone configured in the fake → on-duty time treated as UTC 07:00.
        Assert.Equal(new DateTime(2026, 4, 6, 7, 0, 0, DateTimeKind.Utc), record.OnDutyTimeUtc);
    }

    [Fact]
    public async Task GenerateForShift_IncumbentOnOwnAssignedPosition_MarksRecordAssigned()
    {
        // Arrange - incumbent (employee 200) works crew position 10, which maps to
        // staffable position 500, and employee 200 is assigned to staffable position 500.
        var shiftDef = CreateActiveShiftDef();
        var onDutyRepo = new FakeOnDutyRecordRepository();

        var staffablePositionCtrlNbr = ControlNumber.Create(500);
        var crewPosition = CrewPosition.Create(
            ControlNumber.Create(20), ControlNumber.Create(30), 1, staffablePositionCtrlNbr);
        var crewPositions = new Dictionary<ControlNumber, CrewPosition>
        {
            [ControlNumber.Create(10)] = crewPosition
        };
        var assignment = PositionAssignment.Create(
            staffablePositionCtrlNbr, ControlNumber.Create(200), "Direct");

        var templates = new List<AssignmentDto>
        {
            new(ControlNumber.Create(130), ControlNumber.Create(1), null,
                "TY-101", "Pool Turn 101",
                new TimeOnly(7, 0), new TimeOnly(15, 0),
                "Test Group", "TG",
                [
                    new CrewPositionDto(ControlNumber.Create(10), ControlNumber.Create(200), 1, "Engineer")
                ])
        };

        var sut = CreateSut(
            new FakeAssignmentQueryService(templates),
            new FakeShiftDefinitionRepository(shiftDef),
            new FakeShiftInstanceRepository(),
            new FakeWorkInstanceRepository(),
            new FakeDepartmentRepository(),
            onDutyRecords: onDutyRepo,
            crewPositions: new FakeCrewPositionRepository(crewPositions),
            positionAssignments: new FakePositionAssignmentRepository([assignment]));

        // Act
        await sut.GenerateForShiftAsync(
            ControlNumber.Create(1),
            shiftDef.CtrlNbr,
            new DateOnly(2026, 4, 6),  // Monday
            ct: TestContext.Current.CancellationToken);

        // Assert - the record is flagged assigned because the employee works their own position
        var record = Assert.Single(onDutyRepo.Added);
        Assert.Equal(200, record.EmployeeCtrlNbr.Value);
        Assert.True(record.IsAssigned);
    }

    [Fact]
    public async Task GenerateForShift_IncumbentCoveringAnotherPosition_LeavesRecordUnassigned()
    {
        // Arrange - incumbent (employee 200) works crew position 10 (staffable position 500),
        // but the employee's only assignment is to a different staffable position (600).
        var shiftDef = CreateActiveShiftDef();
        var onDutyRepo = new FakeOnDutyRecordRepository();

        var crewPosition = CrewPosition.Create(
            ControlNumber.Create(20), ControlNumber.Create(30), 1, ControlNumber.Create(500));
        var crewPositions = new Dictionary<ControlNumber, CrewPosition>
        {
            [ControlNumber.Create(10)] = crewPosition
        };
        var assignment = PositionAssignment.Create(
            ControlNumber.Create(600), ControlNumber.Create(200), "Direct");

        var templates = new List<AssignmentDto>
        {
            new(ControlNumber.Create(130), ControlNumber.Create(1), null,
                "TY-101", "Pool Turn 101",
                new TimeOnly(7, 0), new TimeOnly(15, 0),
                "Test Group", "TG",
                [
                    new CrewPositionDto(ControlNumber.Create(10), ControlNumber.Create(200), 1, "Engineer")
                ])
        };

        var sut = CreateSut(
            new FakeAssignmentQueryService(templates),
            new FakeShiftDefinitionRepository(shiftDef),
            new FakeShiftInstanceRepository(),
            new FakeWorkInstanceRepository(),
            new FakeDepartmentRepository(),
            onDutyRecords: onDutyRepo,
            crewPositions: new FakeCrewPositionRepository(crewPositions),
            positionAssignments: new FakePositionAssignmentRepository([assignment]));

        // Act
        await sut.GenerateForShiftAsync(
            ControlNumber.Create(1),
            shiftDef.CtrlNbr,
            new DateOnly(2026, 4, 6),  // Monday
            ct: TestContext.Current.CancellationToken);

        // Assert - covering another position is not "assigned"
        var record = Assert.Single(onDutyRepo.Added);
        Assert.False(record.IsAssigned);
    }

    [Fact]
    public async Task GenerateForShift_AllVacantSlots_CreatesNoOnDutyRecords()
    {
        // Arrange - assignment with only vacant positions (no incumbents)
        var shiftDef = CreateActiveShiftDef();
        var onDutyRepo = new FakeOnDutyRecordRepository();

        var templates = new List<AssignmentDto>
        {
            new(ControlNumber.Create(130), ControlNumber.Create(1), null,
                "TY-101", "Pool Turn 101",
                new TimeOnly(7, 0), new TimeOnly(15, 0),
                "Test Group", "TG",
                [
                    new CrewPositionDto(ControlNumber.Create(10), null, 1, "Engineer"),
                    new CrewPositionDto(ControlNumber.Create(11), null, 2, "Conductor")
                ])
        };

        var sut = CreateSut(
            new FakeAssignmentQueryService(templates),
            new FakeShiftDefinitionRepository(shiftDef),
            new FakeShiftInstanceRepository(),
            new FakeWorkInstanceRepository(),
            new FakeDepartmentRepository(),
            onDutyRecords: onDutyRepo);

        // Act
        await sut.GenerateForShiftAsync(
            ControlNumber.Create(1),
            shiftDef.CtrlNbr,
            new DateOnly(2026, 4, 6),  // Monday
            ct: TestContext.Current.CancellationToken);

        // Assert - vacancies do not produce on-duty records in this increment
        Assert.Empty(onDutyRepo.Added);
    }
}




