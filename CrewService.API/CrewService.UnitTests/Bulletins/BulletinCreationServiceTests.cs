using CrewService.Application.Crews;
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

namespace CrewService.UnitTests.Bulletins;

public class BulletinCreationServiceTests
{
    // shared control numbers
    private static readonly ControlNumber WorkAreaCtrlNbr   = ControlNumber.Create(1);
    private static readonly ControlNumber CraftCtrlNbr      = ControlNumber.Create(2);
    private static readonly ControlNumber CraftRoleCtrlNbr  = ControlNumber.Create(3);
    private static readonly ControlNumber CrewCtrlNbr       = ControlNumber.Create(4);
    private static readonly ControlNumber EmployeeCtrlNbr   = ControlNumber.Create(5);

    // ── helpers ──────────────────────────────────────────────────────────────

    private static BulletinRule MakeRule() =>
        BulletinRule.Create(CraftCtrlNbr, 48, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), 5, new TimeSpan(8, 0, 0), 0);

    private static CraftRole MakeCraftRole() =>
        CraftRole.Create(CraftCtrlNbr, "ENG", "Engineer");

    private static Crew MakeCrew() =>
        Crew.Create("REGULAR", WorkAreaCtrlNbr, "Crew A");

    private static CrewPosition MakeCrewPosition(ControlNumber staffablePositionCtrlNbr) =>
        CrewPosition.Create(CrewCtrlNbr, CraftRoleCtrlNbr, 1, staffablePositionCtrlNbr);

    private static CrewsAppService BuildService(FakeOrchestrationUnitOfWork uow) =>
        new(new FakeUowFactory(uow), NullLogger<CrewsAppService>.Instance);

    // ── CreateCrewPositionAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateCrewPosition_WithRule_CreatesBulletinAndVacancy()
    {
        var uow = new FakeOrchestrationUnitOfWork(bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew());
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
        var uow = new FakeOrchestrationUnitOfWork(bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew());
        var sut = BuildService(uow);

        await sut.CreateCrewPositionAsync(CrewCtrlNbr.Value, CraftRoleCtrlNbr.Value, 1,
            TestContext.Current.CancellationToken);

        Assert.Equal("POSITION_CREATED", uow.FakeVacancies.AddedEntities[0].VacancyReasonCode);
    }

    [Fact]
    public async Task CreateCrewPosition_WithoutRule_NoBulletinCreated()
    {
        var uow = new FakeOrchestrationUnitOfWork(bulletinRule: null, craftRole: MakeCraftRole(), crew: MakeCrew());
        var sut = BuildService(uow);

        await sut.CreateCrewPositionAsync(CrewCtrlNbr.Value, CraftRoleCtrlNbr.Value, 1,
            TestContext.Current.CancellationToken);

        Assert.Empty(uow.FakeVacancies.AddedEntities);
        Assert.Empty(uow.FakeBulletins.AddedEntities);
        Assert.True(uow.Committed);
    }

    // ── EndCrewIncumbencyAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EndCrewIncumbency_WithRule_CreatesBulletinAndVacancy()
    {
        var staffPos    = StaffablePosition.Create(StaffablePositionType.Crew);
        var crewPos     = MakeCrewPosition(staffPos.CtrlNbr);
        var incumbency  = CrewIncumbency.Create(crewPos.CtrlNbr, EmployeeCtrlNbr, DateTime.UtcNow.AddDays(-1));

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPosition: crewPos, incumbency: incumbency);

        var sut = BuildService(uow);

        await sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow,
            TestContext.Current.CancellationToken);

        Assert.Single(uow.FakeVacancies.AddedEntities);
        Assert.Single(uow.FakeBulletins.AddedEntities);
        Assert.True(uow.Committed);
    }

    [Fact]
    public async Task EndCrewIncumbency_WithRule_VacancyReasonIsIncumbentVacated()
    {
        var staffPos   = StaffablePosition.Create(StaffablePositionType.Crew);
        var crewPos    = MakeCrewPosition(staffPos.CtrlNbr);
        var incumbency = CrewIncumbency.Create(crewPos.CtrlNbr, EmployeeCtrlNbr, DateTime.UtcNow.AddDays(-1));

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPosition: crewPos, incumbency: incumbency);

        var sut = BuildService(uow);

        await sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow,
            TestContext.Current.CancellationToken);

        Assert.Equal("INCUMBENT_VACATED", uow.FakeVacancies.AddedEntities[0].VacancyReasonCode);
    }

    [Fact]
    public async Task EndCrewIncumbency_WithoutRule_NoBulletinCreated()
    {
        var staffPos   = StaffablePosition.Create(StaffablePositionType.Crew);
        var crewPos    = MakeCrewPosition(staffPos.CtrlNbr);
        var incumbency = CrewIncumbency.Create(crewPos.CtrlNbr, EmployeeCtrlNbr, DateTime.UtcNow.AddDays(-1));

        var uow = new FakeOrchestrationUnitOfWork(
            bulletinRule: null, craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPosition: crewPos, incumbency: incumbency);

        var sut = BuildService(uow);

        await sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow,
            TestContext.Current.CancellationToken);

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
            bulletinRule: MakeRule(), craftRole: MakeCraftRole(), crew: MakeCrew(),
            crewPosition: crewPos, incumbency: incumbency, positionAssignment: assignment);

        var sut = BuildService(uow);

        await sut.EndCrewIncumbencyAsync(incumbency.CtrlNbr, DateTime.UtcNow,
            TestContext.Current.CancellationToken);

        Assert.Contains(assignment, uow.FakePositionAssignments.RemovedEntities);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Fake infrastructure
    // ═══════════════════════════════════════════════════════════════════════════

    private sealed class FakeUowFactory(FakeOrchestrationUnitOfWork uow) : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(OrchestrationUnitOfWorkOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IOrchestrationUnitOfWork>(uow);
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

    private sealed class FakeOrchestrationUnitOfWork : IOrchestrationUnitOfWork
    {
        public bool Committed { get; private set; }

        public FakeVacancyRepo            FakeVacancies          { get; } = new();
        public FakeBulletinRepo           FakeBulletins          { get; } = new();
        public FakePositionAssignmentRepo FakePositionAssignments { get; }

        private readonly FakeCrewRepo            _crews;
        private readonly FakeCrewPositionRepo     _crewPositions;
        private readonly FakeCrewIncumbencyRepo   _incumbencies;
        private readonly FakeCraftRoleRepo        _craftRoles;
        private readonly FakeBulletinRuleRepo     _bulletinRules;
        private readonly FakeStaffablePositionRepo _staffablePositions = new();

        public FakeOrchestrationUnitOfWork(
            BulletinRule?        bulletinRule,
            CraftRole?           craftRole,
            Crew?                crew,
            CrewPosition?        crewPosition      = null,
            CrewIncumbency?      incumbency        = null,
            PositionAssignment?  positionAssignment = null)
        {
            _crews          = new FakeCrewRepo(crew);
            _crewPositions  = new FakeCrewPositionRepo(crewPosition);
            _incumbencies   = new FakeCrewIncumbencyRepo(incumbency);
            _craftRoles     = new FakeCraftRoleRepo(craftRole);
            _bulletinRules  = new FakeBulletinRuleRepo(bulletinRule);
            FakePositionAssignments = new FakePositionAssignmentRepo(positionAssignment);
        }

        public string CorrelationId  => "test";
        public string OrchestrationId => "test";

        public ICrewRepository             Crews              => _crews;
        public ICrewPositionRepository     CrewPositions      => _crewPositions;
        public ICrewIncumbencyRepository   CrewIncumbencies   => _incumbencies;
        public ICraftRoleRepository        CraftRoles         => _craftRoles;
        public IBulletinRuleRepository     BulletinRules      => _bulletinRules;
        public IPositionVacancyRepository  PositionVacancies  => FakeVacancies;
        public IBulletinRepository         Bulletins          => FakeBulletins;
        public IPositionAssignmentRepository PositionAssignments => FakePositionAssignments;
        public IStaffablePositionRepository StaffablePositions  => _staffablePositions;

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
        public IRosterBoardRepository                    RosterBoards                 => throw new NotImplementedException();
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
        public ISeniorityMovePolicyRepository            SeniorityMovePolicies        => throw new NotImplementedException();
        public ISeniorityMoveRepository                  SeniorityMoves               => throw new NotImplementedException();
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
        public IRosterRepository                         Rosters                      => null!;
        public ISeniorityStateRepository                 SeniorityStates              => null!;
        public IGroupTypeRepository                      GroupTypes                   => null!;
        public IDynamicGroupRepository                   DynamicGroups                => null!;
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
