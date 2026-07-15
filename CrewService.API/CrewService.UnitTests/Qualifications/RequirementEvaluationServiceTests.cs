using CrewService.Application.Qualifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;using CrewService.Domain.Models.Seniority;
using Xunit;

namespace CrewService.UnitTests.Qualifications;

public sealed class RequirementEvaluationServiceTests
{
    [Fact]
    public async Task EvaluateAsync_WhenAllPrerequisitesSatisfied_ReturnsAllSatisfiedWithAchievedAt()
    {
        var employeeCtrlNbr = ControlNumber.Create(10);
        var parentCtrlNbr = ControlNumber.Create(20);

        var qualificationType = QualificationType.Create(
            parentCtrlNbr,
            "FOREMAN",
            "Foreman",
            evaluationStrategy: EvaluationStrategies.ActivityCount,
            expirationMonths: 12,
            isBlocking: true);

        var requirement = qualificationType.AddRequirement(
            requirementKind: RequirementKinds.ActivityCount,
            threshold: 90,
            thresholdUnit: ThresholdUnits.Count,
            description: "90 trips required");

        var prerequisiteRepository = new FakeQualificationRequirementRepository([requirement]);
        var qualificationRepository = new FakeEmployeeQualificationRepository();

        var sut = new RequirementEvaluationService(
            new FakeRequirementEvalUoWFactory(prerequisiteRepository, qualificationRepository),
            [new AlwaysSatisfiedEvaluator(RequirementKinds.ActivityCount, "90 qualifying on-duty records")]);

        var result = await sut.EvaluateAsync(
            employeeCtrlNbr,
            qualificationType,
            TestContext.Current.CancellationToken);

        Assert.True(result.AllSatisfied);
        Assert.NotNull(result.AchievedAtUtc);
        Assert.NotNull(result.ExpiresAtUtc); // 12-month expiry configured

        var check = Assert.Single(result.Results);
        Assert.True(check.IsSatisfied);
        Assert.Equal(RequirementKinds.ActivityCount, check.Kind);

        // Pure compute -- no DB rows written
        Assert.Empty(qualificationRepository.AddedQualifications);
    }

    private sealed class AlwaysSatisfiedEvaluator(string kind, string description) : IRequirementEvaluator
    {
        public string Kind { get; } = kind;

        public Task<EvaluationResult> EvaluateAsync(ControlNumber employeeCtrlNbr, QualificationRequirement rule, CancellationToken ct = default)
            => Task.FromResult(EvaluationResult.Satisfied(description));
    }

    private sealed class FakeRequirementEvalUoW(
        IQualificationRequirementRepository qualificationRequirements,
        IEmployeeQualificationRepository employeeQualifications) : IOrchestrationUnitOfWork
    {
        public string CorrelationId => string.Empty;
        public string OrchestrationId => string.Empty;
        public IQualificationRequirementRepository QualificationRequirements => qualificationRequirements;
        public IEmployeeQualificationRepository EmployeeQualifications => employeeQualifications;
        public IEmployeeQualificationSuspensionRepository QualificationSuspensions => new FakeEmptySuspensionRepo();
        public INotificationTypeConfigRepository NotificationTypeConfigs => null!;
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
        public IPositionAssignmentRepository PositionAssignments => null!;
        public IBoardCascadePolicyRepository BoardCascadePolicies => null!;
        public IRequiredPositionsStrategyRepository RequiredPositionsStrategies => throw new NotImplementedException();
        public ICraftRequiredPositionsStrategyRepository CraftRequiredPositionsStrategies => throw new NotImplementedException();
        public IRosterBoardRepository RosterBoards => null!;
        public ICrewRepository Crews => null!;
        public ICrewPositionRepository CrewPositions => null!;
        public ICrewIncumbencyRepository CrewIncumbencies => null!;
        public ICrewAssignmentRepository CrewAssignments => null!;
        public ICrewAttachmentInstanceRepository CrewAttachmentInstances => null!;
        public IAssignmentRepository Assignments => null!;
        public IAssignmentScheduleRepository AssignmentSchedules => null!;
        public IDepartmentRepository Departments => null!;
        public ICraftRoleRepository CraftRoles => null!;
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
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IUserParentAssignmentRepository UserParentAssignments => null!;
        public IInvitationRepository Invitations => null!;
        public IPayrollTierRepository PayrollTiers => null!;
        public ISeniorityStateVacancyConfigRepository    SeniorityStateVacancyConfigs => throw new NotImplementedException();
        public IPendingSeniorityStateChangeRepository    PendingSeniorityStateChanges => throw new NotImplementedException();
    }

    private sealed class FakeRequirementEvalUoWFactory(
        IQualificationRequirementRepository qualificationRequirements,
        IEmployeeQualificationRepository employeeQualifications) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(
            OrchestrationUnitOfWorkOptions? options = null, CancellationToken ct = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(
                new FakeRequirementEvalUoW(qualificationRequirements, employeeQualifications));
    }

    private sealed class FakeEmptySuspensionRepo
        : FakeRepositoryBase<EmployeeQualificationSuspension>, IEmployeeQualificationSuspensionRepository
    {
        public Task<EmployeeQualificationSuspension?> GetActiveByEmployeeAndTypeAsync(
            ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<EmployeeQualificationSuspension?>(null);

        public Task<List<EmployeeQualificationSuspension>> GetByEmployeeCtrlNbrAsync(
            ControlNumber employeeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(new List<EmployeeQualificationSuspension>());
    }

    private abstract class FakeRepositoryBase<TEntity> : IRepository<TEntity> where TEntity : Entity
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

    private sealed class FakeQualificationRequirementRepository(IReadOnlyList<QualificationRequirement> prerequisites)
        : FakeRepositoryBase<QualificationRequirement>, IQualificationRequirementRepository
    {
        public Task<List<QualificationRequirement>> GetByQualificationTypeCtrlNbrAsync(ControlNumber qualificationTypeCtrlNbr)
            => Task.FromResult(prerequisites.Where(p => p.QualificationTypeCtrlNbr == qualificationTypeCtrlNbr).ToList());
    }

    private sealed class FakeEmployeeQualificationRepository
        : FakeRepositoryBase<EmployeeQualification>, IEmployeeQualificationRepository
    {
        public List<EmployeeQualification> AddedQualifications { get; } = [];

        public override Task AddAsync(EmployeeQualification entity, CancellationToken ct = default)
        {
            AddedQualifications.Add(entity);
            return Task.CompletedTask;
        }

        public Task<List<EmployeeQualification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
            => Task.FromResult(new List<EmployeeQualification>());

        public Task<EmployeeQualification?> GetByEmployeeAndTypeAsync(ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr)
            => Task.FromResult<EmployeeQualification?>(null);

        public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
            => Task.FromResult(new List<EmployeeQualification>());

        public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrsAsync(IEnumerable<ControlNumber> employeeCtrlNbrs)
            => Task.FromResult(new List<EmployeeQualification>());

        public Task<List<EmployeeQualification>> GetExpiringBeforeAsync(DateTime cutoffUtc)
            => Task.FromResult(new List<EmployeeQualification>());
    }
}


