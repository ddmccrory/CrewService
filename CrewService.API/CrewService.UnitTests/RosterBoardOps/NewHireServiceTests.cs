using CrewService.Application.Qualifications;
using CrewService.Application.RosterBoardOps;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.RosterBoardOps;

public class NewHireServiceTests
{
    private static readonly ControlNumber EmpCtrlNbr = ControlNumber.Create(10);
    private static readonly ControlNumber CraftCtrlNbr = ControlNumber.Create(100);
    private static readonly ControlNumber TrainingRosterCtrlNbr = ControlNumber.Create(99);
    private static readonly ControlNumber StateCtrlNbr = ControlNumber.Create(1);
    private static readonly ControlNumber RegQualCtrlNbr = ControlNumber.Create(50);
    private static readonly DateTime HireDate = new DateTime(2025, 1, 15);

    [Fact]
    public async Task OnboardAsync_CreatesSeniorityOnTrainingRoster()
    {
        var uow = new FakeOrchestrationUnitOfWork(CraftCtrlNbr);
        var sut = BuildService(uow);

        await sut.OnboardAsync(EmpCtrlNbr, CraftCtrlNbr, TrainingRosterCtrlNbr, StateCtrlNbr, HireDate, null, ct: TestContext.Current.CancellationToken);

        var added = Assert.Single(uow.FakeSeniority.AddedEntities);
        Assert.Equal(TrainingRosterCtrlNbr, added.RosterCtrlNbr);
        Assert.Equal(EmpCtrlNbr, added.EmployeeCtrlNbr);
        Assert.False(added.LastActiveRoster);
        Assert.Equal(HireDate.Date, added.RosterDate.Date);
    }

    [Fact]
    public async Task OnboardAsync_WithRegQual_CreatesPendingCertification()
    {
        var uow = new FakeOrchestrationUnitOfWork(CraftCtrlNbr);
        var sut = BuildService(uow);

        await sut.OnboardAsync(EmpCtrlNbr, CraftCtrlNbr, TrainingRosterCtrlNbr, StateCtrlNbr, HireDate, RegQualCtrlNbr, ct: TestContext.Current.CancellationToken);

        var cert = Assert.Single(uow.FakeCertifications.AddedEntities);
        Assert.Equal(EmpCtrlNbr, cert.EmployeeCtrlNbr);
        Assert.Equal(RegQualCtrlNbr, cert.RegulatoryQualificationCtrlNbr);
        Assert.Equal("Pending", cert.Status);
    }

    [Fact]
    public async Task OnboardAsync_WithoutRegQual_NoCertificationCreated()
    {
        var uow = new FakeOrchestrationUnitOfWork(CraftCtrlNbr);
        var sut = BuildService(uow);

        await sut.OnboardAsync(EmpCtrlNbr, CraftCtrlNbr, TrainingRosterCtrlNbr, StateCtrlNbr, HireDate, regulatoryQualificationCtrlNbr: null, ct: TestContext.Current.CancellationToken);

        Assert.Empty(uow.FakeCertifications.AddedEntities);
    }

    [Fact]
    public async Task OnboardAsync_AddsEmployeeToNewHireBoard()
    {
        var uow = new FakeOrchestrationUnitOfWork(CraftCtrlNbr);
        var sut = BuildService(uow);

        await sut.OnboardAsync(EmpCtrlNbr, CraftCtrlNbr, TrainingRosterCtrlNbr, StateCtrlNbr, HireDate, null, ct: TestContext.Current.CancellationToken);

        var board = uow.FakeBoards.Boards.Single(b => b.BoardType == BoardType.NewHire);
        Assert.Single(board.Positions);
        Assert.Equal(EmpCtrlNbr, board.Positions[0].EmployeeCtrlNbr);
    }

    [Fact]
    public async Task OnboardAsync_CommitsUoW()
    {
        var uow = new FakeOrchestrationUnitOfWork(CraftCtrlNbr);
        var sut = BuildService(uow);

        await sut.OnboardAsync(EmpCtrlNbr, CraftCtrlNbr, TrainingRosterCtrlNbr, StateCtrlNbr, HireDate, null, ct: TestContext.Current.CancellationToken);

        Assert.True(uow.Committed);
    }

    [Fact]
    public async Task OnboardAsync_WhenNoBoardExists_DoesNotThrow()
    {
        // Board is associated by craft but no NewHire board exists
        var uow = new FakeOrchestrationUnitOfWork(CraftCtrlNbr, includeNewHireBoard: false);
        var sut = BuildService(uow);

        // Should not throw — new hire board is optional
        await sut.OnboardAsync(EmpCtrlNbr, CraftCtrlNbr, TrainingRosterCtrlNbr, StateCtrlNbr, HireDate, null, ct: TestContext.Current.CancellationToken);

        Assert.True(uow.Committed);
    }

    // ────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────

    private static NewHireService BuildService(FakeOrchestrationUnitOfWork uow)
    {
        var uowFactory = new FakeUowFactory(uow);
        var requirementEvalSvc = new RequirementEvaluationService(uowFactory, []);
        var qualReactiveSvc = new QualificationReactiveService(uowFactory, requirementEvalSvc);
        return new NewHireService(uowFactory, qualReactiveSvc);
    }

    // ────────────────────────────────────────────────────────────
    // Fakes
    // ────────────────────────────────────────────────────────────

    private sealed class FakeUowFactory(FakeOrchestrationUnitOfWork uow) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(OrchestrationUnitOfWorkOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(uow);
    }

    private sealed class FakeOrchestrationUnitOfWork : IOrchestrationUnitOfWork
    {
        public bool Committed { get; private set; }
        public FakeSeniorityRepository FakeSeniority { get; } = new();
        public FakeCertificationRepository FakeCertifications { get; } = new();
        public FakeBoardRepository FakeBoards { get; }

        public FakeOrchestrationUnitOfWork(ControlNumber craftCtrlNbr, bool includeNewHireBoard = true)
        {
            FakeBoards = new FakeBoardRepository(craftCtrlNbr, includeNewHireBoard);
        }

        public string CorrelationId => "test";
        public string OrchestrationId => "test";

        public ISeniorityRepository Seniority => FakeSeniority;
        public IEmployeeCertificationRepository EmployeeCertifications => FakeCertifications;
        public IBoardCascadePolicyRepository BoardCascadePolicies => throw new NotImplementedException();
        public IRosterBoardRepository RosterBoards => FakeBoards;
        public IAbsenceRequestRepository AbsenceRequests => throw new NotImplementedException();
        public IVacancyImpactRepository VacancyImpacts => throw new NotImplementedException();
        public ISafetyObservationRepository SafetyObservations => throw new NotImplementedException();
        public ISafetyObservationResolutionRepository SafetyResolutions => throw new NotImplementedException();
        public ISafetyCategoryRepository SafetyCategories => throw new NotImplementedException();
        public IRailroadInformationRepository RailroadInformation => throw new NotImplementedException();
        public IRailroadInformationReadReceiptRepository RailroadInformationReadReceipts => throw new NotImplementedException();
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
        public IRoleRepository Roles => throw new NotImplementedException();
        public IFeatureRepository Features => throw new NotImplementedException();
        public IPermissionRepository Permissions => throw new NotImplementedException();
        public IPositionVacancyRepository PositionVacancies => throw new NotImplementedException();
        public IBulletinRepository Bulletins => throw new NotImplementedException();
        public IBulletinBidRepository BulletinBids => throw new NotImplementedException();
        public IDispatchProjectionRepository DispatchProjections => throw new NotImplementedException();
        public IDispatchDecisionLogRepository DispatchDecisionLogs => throw new NotImplementedException();
        public IDispatchOverrideRepository DispatchOverrides => throw new NotImplementedException();
        public IEmployeeBookingRepository EmployeeBookings => throw new NotImplementedException();
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

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Committed = true;
            return Task.CompletedTask;
        }
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }

        // Unused but required by interface
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
        public ISeniorityStateRepository SeniorityStates => null!;
        public IGroupTypeRepository GroupTypes => null!;
        public IDynamicGroupRepository DynamicGroups => null!;
        public IGroupAttributeDefinitionRepository AttributeDefinitions => null!;
        public IGroupAttributeValueRepository AttributeValues => null!;
        public IStaffablePositionRepository StaffablePositions => _fakeStaffablePositions;
        private readonly FakeStaffablePositionRepository _fakeStaffablePositions = new();
        public IPositionAssignmentRepository PositionAssignments => null!;
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
        public IQualificationTypeRepository QualificationTypes => null!;
        public IQualificationRequirementRepository QualificationRequirements => null!;
        public IEmployeeQualificationRepository EmployeeQualifications => null!;
        public ICertificationRevocationRepository CertificationRevocations => null!;
        public IDrugAlcoholActionRepository DrugAlcoholActions => null!;
        public IDrugAlcoholTestRepository DrugAlcoholTests => null!;
        public IEmployeeCertificationReadRepository EmployeeCertificationReads => null!;
        public IFraCertificationCheckConfigRepository FraCertificationCheckConfigs => null!;
        public IFraCertificationConfigRepository FraCertificationConfigs => null!;
        public IFraDutyTourRepository FraDutyTours => null!;
        public IRegulatoryQualificationRepository RegulatoryQualifications => null!;
        public IRegulatoryStandardRepository RegulatoryStandards => null!;
        public IVoluntaryReferralRepository VoluntaryReferrals => null!;
    }

    private abstract class FakeRepositoryBase<TEntity> : IRepository<TEntity> where TEntity : Entity
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
        public virtual void Add(TEntity entity) { AddedEntities.Add(entity); }
        public virtual void Update(TEntity entity) { }
        public virtual void Remove(TEntity entity) { }
    }

    private sealed class FakeSeniorityRepository : FakeRepositoryBase<Seniority>, ISeniorityRepository
    {
        public Task<List<Seniority>> GetByRosterCtrlNbrAsync(ControlNumber rosterCtrlNbr) => Task.FromResult(new List<Seniority>());
        public Task<List<Seniority>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr) => Task.FromResult(new List<Seniority>());
    }

    private sealed class FakeCertificationRepository : IEmployeeCertificationRepository
    {
        public List<EmployeeCertification> AddedEntities { get; } = [];
        public Task<List<EmployeeCertification>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<EmployeeCertification>());
        public Task<List<EmployeeCertification>> GetAllWithChecksAsync(CancellationToken ct = default) => Task.FromResult(new List<EmployeeCertification>());
        public Task<IReadOnlyList<EmployeeCertification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<EmployeeCertification>>([]);
        public Task<EmployeeCertification?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<EmployeeCertification?>(null);
        public Task<EmployeeCertification?> GetByCtrlNbrWithChecksAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<EmployeeCertification?>(null);
        public Task<EmployeeCertification?> GetByEligibilityCheckCtrlNbrWithChecksAsync(ControlNumber eligibilityCheckCtrlNbr, CancellationToken ct = default) => Task.FromResult<EmployeeCertification?>(null);
        public Task<EmployeeCertification?> GetByEmployeeAndRegulatoryQualAsync(ControlNumber employeeCtrlNbr, ControlNumber regulatoryQualCtrlNbr, CancellationToken ct = default) => Task.FromResult<EmployeeCertification?>(null);
        public Task AddAsync(EmployeeCertification cert, CancellationToken ct = default) { AddedEntities.Add(cert); return Task.CompletedTask; }
        public void Add(EmployeeCertification cert) { AddedEntities.Add(cert); }
        public Task UpdateAsync(EmployeeCertification cert, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeBoardRepository : FakeRepositoryBase<RosterBoard>, IRosterBoardRepository
    {
        public List<RosterBoard> Boards { get; }

        public FakeBoardRepository(ControlNumber craftCtrlNbr, bool includeNewHireBoard)
        {
            Boards = [];
            if (includeNewHireBoard)
            {
                Boards.Add(RosterBoard.Create(craftCtrlNbr, ControlNumber.Create(99), "New Hires", BoardType.NewHire));
            }
        }

        public override void Update(RosterBoard entity) { }
        public Task<IReadOnlyList<RosterBoard>> GetActiveByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RosterBoard>>(Boards);
        public Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RosterBoard>>(Boards.Where(b => b.CraftCtrlNbr == craftCtrlNbr).ToList());
        public Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> craftCtrlNbrs, CancellationToken ct = default)
        {
            var set = craftCtrlNbrs.ToHashSet();
            return Task.FromResult<IReadOnlyList<RosterBoard>>(Boards.Where(b => set.Contains(b.CraftCtrlNbr)).ToList());
        }
    }

    private sealed class FakeQualificationTypeRepository : FakeRepositoryBase<QualificationType>, IQualificationTypeRepository
    {
        public Task<List<QualificationType>> GetActiveByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr) => Task.FromResult(new List<QualificationType>());
        public Task<List<QualificationType>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr) => Task.FromResult(new List<QualificationType>());
        public Task<QualificationType?> GetByCodeAsync(ControlNumber parentCtrlNbr, string code) => Task.FromResult<QualificationType?>(null);
        public Task<List<QualificationType>> GetActiveByParentCtrlNbrAsync(ControlNumber parentCtrlNbr) => Task.FromResult(new List<QualificationType>());
    }

    private sealed class FakeQualificationRequirementRepository : FakeRepositoryBase<QualificationRequirement>, IQualificationRequirementRepository
    {
        public Task<List<QualificationRequirement>> GetByQualificationTypeCtrlNbrAsync(ControlNumber qualificationTypeCtrlNbr) => Task.FromResult(new List<QualificationRequirement>());
    }

    private sealed class FakeEmployeeQualificationRepository : FakeRepositoryBase<EmployeeQualification>, IEmployeeQualificationRepository
    {
        public Task<List<EmployeeQualification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr) => Task.FromResult(new List<EmployeeQualification>());
        public Task<EmployeeQualification?> GetByEmployeeAndTypeAsync(ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr) => Task.FromResult<EmployeeQualification?>(null);
        public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr) => Task.FromResult(new List<EmployeeQualification>());
        public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrsAsync(IEnumerable<ControlNumber> employeeCtrlNbrs) => Task.FromResult(new List<EmployeeQualification>());
        public Task<List<EmployeeQualification>> GetExpiringBeforeAsync(DateTime cutoffUtc) => Task.FromResult(new List<EmployeeQualification>());
    }

    private sealed class FakeStaffablePositionRepository : FakeRepositoryBase<StaffablePosition>, IStaffablePositionRepository
    {
        public Task<List<StaffablePosition>> GetByPositionTypeAsync(string positionType) => Task.FromResult(new List<StaffablePosition>());
    }
}
