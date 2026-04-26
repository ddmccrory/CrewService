using CrewService.Application.DailyOperations;
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

        public Task<List<WorkInstance>> GetByWorkAreaAndDateRangeAsync(
            ControlNumber workAreaGroupCtrlNbr, DateTime startUtc, DateTime endUtc)
            => Task.FromResult(new List<WorkInstance>());
    }

    private sealed class FakeDepartmentRepository : FakeRepository<Department>, IDepartmentRepository
    {
        public Task<List<Department>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr)
            => Task.FromResult(new List<Department>());
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
        IDepartmentRepository departments) : IOrchestrationUnitOfWork
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
        public IRosterBoardRepository RosterBoards => null!;
        public ICrewRepository Crews => null!;
        public ICrewPositionRepository CrewPositions => null!;
        public ICrewIncumbencyRepository CrewIncumbencies => null!;
        public ICrewAssignmentRepository CrewAssignments => null!;
        public ICrewAttachmentInstanceRepository CrewAttachmentInstances => null!;
        public IAssignmentRepository Assignments => null!;
        public IAssignmentScheduleRepository AssignmentSchedules => null!;
        public ICraftRoleRepository CraftRoles => null!;
        public ICraftRoleQualificationRepository CraftRoleQualifications => null!;
        public IPositionSlotRepository PositionSlots => null!;
        public ISlotRequirementRepository SlotRequirements => null!;
        public IOnDutyRecordRepository OnDutyRecords => null!;
        public IOffDutyRecordRepository OffDutyRecords => null!;
        public ICraftOperationsPolicyRepository CraftOperationsPolicies => null!;
        public ICraftDisplacementPolicyRepository CraftDisplacementPolicies => null!;
        public IDisplacementCaseRepository DisplacementCases => null!;
        public IDisplacementClaimRepository DisplacementClaims => null!;
        public IBulletinPolicyRepository BulletinPolicies => null!;
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
        public IUserParentAssignmentRepository UserParentAssignments => null!;
        public IInvitationRepository Invitations => null!;
        public IPayrollTierRepository PayrollTiers => null!;
    }

    private sealed class FakeCallSheetUoWFactory(
        IShiftDefinitionRepository shiftDefinitions,
        IShiftInstanceRepository shiftInstances,
        IWorkInstanceRepository workInstances,
        IDepartmentRepository departments) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(
            OrchestrationUnitOfWorkOptions? options = null, CancellationToken ct = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(
                new FakeCallSheetUoW(shiftDefinitions, shiftInstances, workInstances, departments));
    }

    private static CallSheetGenerationService CreateSut(
        IAssignmentQueryService assignmentQuery,
        IShiftDefinitionRepository shiftDefinitions,
        IShiftInstanceRepository shiftInstances,
        IWorkInstanceRepository workInstances,
        IDepartmentRepository departments)
        => new(new FakeCallSheetUoWFactory(shiftDefinitions, shiftInstances, workInstances, departments), assignmentQuery);

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
}




