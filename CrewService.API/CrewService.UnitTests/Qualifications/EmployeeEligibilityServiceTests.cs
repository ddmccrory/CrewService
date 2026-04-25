using CrewService.Application.Qualifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
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
using Xunit;

namespace CrewService.UnitTests.Qualifications;

public sealed class EmployeeEligibilityServiceTests
{
    [Fact]
    public async Task CheckEligibilityAsync_WhenCraftMembershipMissing_ReturnsBlockingReason()
    {
        var employeeCtrlNbr = ControlNumber.Create(10);
        var positionSlotCtrlNbr = ControlNumber.Create(100);
        var craftCtrlNbr = ControlNumber.Create(200);
        var craftRole = CraftRole.Create(craftCtrlNbr, "TRN", "Trainman");

        var slotRequirementRepository = new FakeSlotRequirementRepository([
            SlotRequirement.Create(positionSlotCtrlNbr, 1, craftRoleCtrlNbr: craftRole.CtrlNbr)
        ]);

        var sut = CreateSut(
            slotRequirementRepository,
            new FakePositionSlotRepository(),
            new FakeQualificationTypeRepository(),
            new FakeEmployeeQualificationRepository(),
            new FakeCraftRoleRepository([craftRole]),
            new FakeCraftRoleQualificationRepository(),
            new FakeSeniorityRepository([]),
            new FakeRosterRepository([Roster.Create(craftCtrlNbr, ControlNumber.Create(900), null, "Trainman", "Trainmen", 1)]));

        var result = await sut.CheckEligibilityAsync(
            employeeCtrlNbr,
            positionSlotCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsEligible);
        Assert.Contains(result.BlockingReasons, r => r.RuleCode == "CRAFT_MEMBERSHIP_MISSING");
    }

    [Fact]
    public async Task CheckEligibilityAsync_WhenCraftMembershipPresent_AllowsEligibility()
    {
        var employeeCtrlNbr = ControlNumber.Create(10);
        var positionSlotCtrlNbr = ControlNumber.Create(100);
        var craftCtrlNbr = ControlNumber.Create(200);
        var craftRole = CraftRole.Create(craftCtrlNbr, "TRN", "Trainman");
        var roster = Roster.Create(craftCtrlNbr, ControlNumber.Create(900), null, "Trainman", "Trainmen", 1);

        var slotRequirementRepository = new FakeSlotRequirementRepository([
            SlotRequirement.Create(positionSlotCtrlNbr, 1, craftRoleCtrlNbr: craftRole.CtrlNbr)
        ]);

        var seniority = Seniority.Create(
            roster.CtrlNbr,
            employeeCtrlNbr,
            lastActiveRoster: true,
            rosterDate: DateTime.UtcNow.AddDays(-60),
            rank: 1,
            seniorityStateCtrlNbr: ControlNumber.Create(1),
            canTrain: true);

        var sut = CreateSut(
            slotRequirementRepository,
            new FakePositionSlotRepository(),
            new FakeQualificationTypeRepository(),
            new FakeEmployeeQualificationRepository(),
            new FakeCraftRoleRepository([craftRole]),
            new FakeCraftRoleQualificationRepository(),
            new FakeSeniorityRepository([seniority]),
            new FakeRosterRepository([roster]));

        var result = await sut.CheckEligibilityAsync(
            employeeCtrlNbr,
            positionSlotCtrlNbr,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsEligible);
        Assert.Empty(result.BlockingReasons);
    }

    private static EmployeeEligibilityService CreateSut(
        ISlotRequirementRepository slotRequirements,
        IPositionSlotRepository positionSlots,
        IQualificationTypeRepository qualificationTypes,
        IEmployeeQualificationRepository employeeQualifications,
        ICraftRoleRepository craftRoles,
        ICraftRoleQualificationRepository craftRoleQualifications,
        ISeniorityRepository seniority,
        IRosterRepository rosters)
    {
        var uowFactory = new FakeEligibilityUoWFactory(slotRequirements, positionSlots, craftRoles,
            craftRoleQualifications, seniority, rosters, qualificationTypes, employeeQualifications);
        return new EmployeeEligibilityService(uowFactory);
    }

    private sealed class FakeEligibilityUoW(
        ISlotRequirementRepository slotRequirements,
        IPositionSlotRepository positionSlots,
        ICraftRoleRepository craftRoles,
        ICraftRoleQualificationRepository craftRoleQualifications,
        ISeniorityRepository seniority,
        IRosterRepository rosters,
        IQualificationTypeRepository qualificationTypes,
        IEmployeeQualificationRepository employeeQualifications) : IOrchestrationUnitOfWork
    {
        public string CorrelationId => string.Empty;
        public string OrchestrationId => string.Empty;
        public ISlotRequirementRepository SlotRequirements => slotRequirements;
        public IPositionSlotRepository PositionSlots => positionSlots;
        public ICraftRoleRepository CraftRoles => craftRoles;
        public ICraftRoleQualificationRepository CraftRoleQualifications => craftRoleQualifications;
        public ISeniorityRepository Seniority => seniority;
        public IRosterRepository Rosters => rosters;
        public IQualificationTypeRepository QualificationTypes => qualificationTypes;
        public IEmployeeQualificationRepository EmployeeQualifications => employeeQualifications;
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
                public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
        public IEmployeeRepository Employees => null!;
        public IParentRepository Parents => null!;
        public IAddressTypeRepository AddressTypes => null!;
        public IPhoneNumberTypeRepository PhoneNumberTypes => null!;
        public IEmailAddressTypeRepository EmailAddressTypes => null!;
        public IEmploymentStatusRepository EmploymentStatuses => null!;
        public IEmploymentStatusHistoryRepository EmploymentStatusHistory => null!;
        public IEmployeePriorServiceCreditRepository EmployeePriorServiceCredits => null!;
        public ICraftRepository Crafts => null!;
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
        public IDepartmentRepository Departments => null!;
        public IWorkInstanceRepository WorkInstances => null!;
        public IShiftDefinitionRepository ShiftDefinitions => null!;
        public IShiftInstanceRepository ShiftInstances => null!;
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
        public IQualificationRequirementRepository QualificationRequirements => null!;
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
    }

    private sealed class FakeEligibilityUoWFactory(
        ISlotRequirementRepository slotRequirements,
        IPositionSlotRepository positionSlots,
        ICraftRoleRepository craftRoles,
        ICraftRoleQualificationRepository craftRoleQualifications,
        ISeniorityRepository seniority,
        IRosterRepository rosters,
        IQualificationTypeRepository qualificationTypes,
        IEmployeeQualificationRepository employeeQualifications) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(
            OrchestrationUnitOfWorkOptions? options = null, CancellationToken ct = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(
                new FakeEligibilityUoW(slotRequirements, positionSlots, craftRoles,
                    craftRoleQualifications, seniority, rosters, qualificationTypes, employeeQualifications));
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

    private sealed class FakeSlotRequirementRepository(IReadOnlyList<SlotRequirement> requirements)
        : FakeRepositoryBase<SlotRequirement>, ISlotRequirementRepository
    {
        public Task<List<SlotRequirement>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr)
            => Task.FromResult(requirements.Where(r => r.PositionSlotCtrlNbr == positionSlotCtrlNbr).ToList());
    }

    private sealed class FakePositionSlotRepository : FakeRepositoryBase<PositionSlot>, IPositionSlotRepository
    {
        public Task<List<PositionSlot>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr) => Task.FromResult(new List<PositionSlot>());
        public Task<List<PositionSlot>> GetOpenByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr) => Task.FromResult(new List<PositionSlot>());
    }

    private sealed class FakeCraftRoleQualificationRepository : FakeRepositoryBase<CraftRoleQualification>, ICraftRoleQualificationRepository
    {
        public Task<List<CraftRoleQualification>> GetByCraftRoleAsync(ControlNumber craftRoleCtrlNbr) => Task.FromResult(new List<CraftRoleQualification>());
    }

    private sealed class FakeQualificationTypeRepository : FakeRepositoryBase<QualificationType>, IQualificationTypeRepository
    {
        public Task<List<QualificationType>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr) => Task.FromResult(new List<QualificationType>());
        public Task<QualificationType?> GetByCodeAsync(ControlNumber parentCtrlNbr, string code) => Task.FromResult<QualificationType?>(null);
        public Task<List<QualificationType>> GetActiveByParentCtrlNbrAsync(ControlNumber parentCtrlNbr) => Task.FromResult(new List<QualificationType>());
        public Task<List<QualificationType>> GetActiveByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr) => Task.FromResult(new List<QualificationType>());
    }

    private sealed class FakeEmployeeQualificationRepository : FakeRepositoryBase<EmployeeQualification>, IEmployeeQualificationRepository
    {
        public Task<List<EmployeeQualification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr) => Task.FromResult(new List<EmployeeQualification>());
        public Task<EmployeeQualification?> GetByEmployeeAndTypeAsync(ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr) => Task.FromResult<EmployeeQualification?>(null);
        public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr) => Task.FromResult(new List<EmployeeQualification>());
        public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrsAsync(IEnumerable<ControlNumber> employeeCtrlNbrs) => Task.FromResult(new List<EmployeeQualification>());
        public Task<List<EmployeeQualification>> GetExpiringBeforeAsync(DateTime cutoffUtc) => Task.FromResult(new List<EmployeeQualification>());
    }

    private sealed class FakeCraftRoleRepository(IReadOnlyList<CraftRole> craftRoles)
        : FakeRepositoryBase<CraftRole>, ICraftRoleRepository
    {
        public override Task<CraftRole?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<CraftRole?>(craftRoles.SingleOrDefault(c => c.CtrlNbr == ctrlNbr));

        public Task<List<CraftRole>> GetByCraftAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult(craftRoles.Where(c => c.CraftCtrlNbr == craftCtrlNbr).ToList());

        public Task<List<CraftRole>> GetByDepartmentAsync(ControlNumber departmentCtrlNbr)
            => Task.FromResult(new List<CraftRole>());

        public Task<List<CraftRole>> GetByRailroadAsync(ControlNumber railroadCtrlNbr)
            => Task.FromResult(new List<CraftRole>());

        public Task<CraftRole?> GetByCtrlNbrWithQualificationsAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
            => Task.FromResult<CraftRole?>(craftRoles.SingleOrDefault(c => c.CtrlNbr == ctrlNbr));
    }

    private sealed class FakeSeniorityRepository(IReadOnlyList<Seniority> seniorities)
        : FakeRepositoryBase<Seniority>, ISeniorityRepository
    {
        public Task<List<Seniority>> GetByRosterCtrlNbrAsync(ControlNumber rosterCtrlNbr)
            => Task.FromResult(seniorities.Where(s => s.RosterCtrlNbr == rosterCtrlNbr).ToList());

        public Task<List<Seniority>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
            => Task.FromResult(seniorities.Where(s => s.EmployeeCtrlNbr == employeeCtrlNbr).ToList());
    }

    private sealed class FakeRosterRepository(IReadOnlyList<Roster> rosters)
        : FakeRepositoryBase<Roster>, IRosterRepository
    {
        public Task<List<Roster>> GetByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr)
            => Task.FromResult(rosters.Where(r => r.CraftCtrlNbr == craftCtrlNbr).ToList());

        public Task<List<Roster>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> craftCtrlNbrs)
        {
            var set = craftCtrlNbrs.ToHashSet();
            return Task.FromResult(rosters.Where(r => set.Contains(r.CraftCtrlNbr)).ToList());
        }

        public Task<Roster?> GetTrainingRosterByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(rosters.FirstOrDefault(r => r.CraftCtrlNbr == craftCtrlNbr && r.RosterType == RosterType.Training));
    }
}


