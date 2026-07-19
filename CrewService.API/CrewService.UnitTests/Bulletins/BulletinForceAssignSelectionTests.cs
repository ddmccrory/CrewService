using CrewService.Application.Bulletins;
using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Notifications;
using CrewService.Application.Qualifications;
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

namespace CrewService.UnitTests.Bulletins;

/// <summary>
/// Behaviour tests for hierarchy-driven, qualification-filtered, junior-first force-assign
/// candidate selection in <see cref="BulletinsService"/>. Roles are intentionally named
/// "Apprentice"/"Lead" (no "Helper" substring) to prove subordinate-tier selection is driven by
/// tenant-configured <see cref="CraftRole.HierarchyLevel"/>, not hardcoded role names.
/// </summary>
public sealed class BulletinForceAssignSelectionTests
{
    private static readonly ControlNumber CraftCtrlNbr          = ControlNumber.Create(500);
    private static readonly ControlNumber WorkAreaCtrlNbr       = ControlNumber.Create(501);
    private static readonly ControlNumber RosterCtrlNbr         = ControlNumber.Create(502);
    private static readonly ControlNumber SeniorityStateCtrlNbr = ControlNumber.Create(503);

    private static readonly ControlNumber EmpJuniorUnqualified = ControlNumber.Create(801);
    private static readonly ControlNumber EmpMidQualified      = ControlNumber.Create(802);
    private static readonly ControlNumber EmpSeniorQualified   = ControlNumber.Create(803);

    [Fact]
    public async Task GetForceAssignCandidate_PicksJuniorMostQualifiedSubordinate_ByHierarchyNotName()
    {
        // Lead (level 1) is force-assigned from its subordinate tier (Apprentice, level 0) plus the
        // extra board. Names avoid "Helper" to prove the pool is built from HierarchyLevel.
        var leadRole       = CraftRole.Create(CraftCtrlNbr, "LEAD", "Lead", hierarchyLevel: 1);
        var apprenticeRole = CraftRole.Create(CraftCtrlNbr, "APPR", "Apprentice", hierarchyLevel: 0);

        // Lead requires a blocking qualification; only the mid/senior employees hold it.
        var leadQual = QualificationType.Create(ControlNumber.Create(700), "LEADQ", "Lead Certification", isBlocking: true);
        leadRole.AddRequiredQualification(leadQual.CtrlNbr);

        // Crew with one Lead position (the vacancy target) and two Apprentice positions (subordinates).
        var crew          = Crew.Create("REGULAR", WorkAreaCtrlNbr, "Crew A");
        var leadStaffPos  = ControlNumber.Create(900);
        var apprStaffPos1 = ControlNumber.Create(901);
        var apprStaffPos2 = ControlNumber.Create(902);
        var leadPosition  = CrewPosition.Create(crew.CtrlNbr, leadRole.CtrlNbr, 1, leadStaffPos);
        var apprPosition1 = CrewPosition.Create(crew.CtrlNbr, apprenticeRole.CtrlNbr, 2, apprStaffPos1);
        var apprPosition2 = CrewPosition.Create(crew.CtrlNbr, apprenticeRole.CtrlNbr, 3, apprStaffPos2);

        var incumbency1 = CrewIncumbency.Create(apprPosition1.CtrlNbr, EmpJuniorUnqualified, DateTime.UtcNow.AddDays(-5));
        var incumbency2 = CrewIncumbency.Create(apprPosition2.CtrlNbr, EmpMidQualified, DateTime.UtcNow.AddDays(-5));

        // Extra board holds the most-senior qualified employee.
        var board = RosterBoard.Create(CraftCtrlNbr, RosterCtrlNbr, "Extra Board", BoardType.ExtraBoard);
        board.AddPosition(EmpSeniorQualified, 1, ControlNumber.Create(950));

        // Junior-first ordering is by RosterDate desc: newest date = most junior.
        var seniorities = new[]
        {
            MakeSeniority(EmpJuniorUnqualified, DateTime.UtcNow.AddDays(-10)),
            MakeSeniority(EmpMidQualified,      DateTime.UtcNow.AddDays(-100)),
            MakeSeniority(EmpSeniorQualified,   DateTime.UtcNow.AddDays(-1000)),
        };

        var employeeQualifications = new[]
        {
            EmployeeQualification.Create(EmpMidQualified,    leadQual.CtrlNbr, "test", achievedAtUtc: DateTime.UtcNow.AddDays(-200)),
            EmployeeQualification.Create(EmpSeniorQualified, leadQual.CtrlNbr, "test", achievedAtUtc: DateTime.UtcNow.AddDays(-200)),
        };

        var vacancy  = PositionVacancy.Create(WorkAreaCtrlNbr, StaffablePositionType.Crew, leadStaffPos, CraftCtrlNbr, "VACANCY");
        var bulletin = Bulletin.Create(vacancy.CtrlNbr, CraftCtrlNbr, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        var rule     = BulletinRule.Create(
            CraftCtrlNbr, 48, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), 5, new TimeSpan(8, 0, 0), 8,
            ForceAssignSelectionMode.JuniorHelperOrExtraBoard);

        var uow = new ForceAssignFakeUow(
            bulletin: bulletin,
            rule: rule,
            vacancy: vacancy,
            craftRoles: [leadRole, apprenticeRole],
            crewPositionsByStaffPos: new() { [leadStaffPos] = leadPosition },
            crewPositionsByCrew: new() { [crew.CtrlNbr] = [leadPosition, apprPosition1, apprPosition2] },
            crewsByWorkArea: new() { [WorkAreaCtrlNbr] = [crew] },
            incumbenciesByPosition: new()
            {
                [apprPosition1.CtrlNbr] = incumbency1,
                [apprPosition2.CtrlNbr] = incumbency2,
            },
            boardsByCraft: [board],
            seniorities: seniorities,
            craftRoleQualifications: leadRole.RequiredQualifications.ToList(),
            qualificationTypes: [leadQual],
            employeeQualifications: employeeQualifications);

        var sut = BuildBulletins(uow);

        var candidate = await sut.GetForceAssignCandidateAsync(bulletin.CtrlNbr, TestContext.Current.CancellationToken);

        // The most-junior employee (EmpJuniorUnqualified) is skipped for lacking the qualification,
        // so the junior-most *qualified* subordinate (EmpMidQualified) wins over the senior board member.
        Assert.Equal(EmpMidQualified, candidate);
    }

    [Fact]
    public async Task GetForceAssignCandidate_EnforcesNonBlockingRequiredQualification()
    {
        var leadRole       = CraftRole.Create(CraftCtrlNbr, "LEAD", "Lead", hierarchyLevel: 1);
        var apprenticeRole = CraftRole.Create(CraftCtrlNbr, "APPR", "Apprentice", hierarchyLevel: 0);

        var leadQual = QualificationType.Create(
            ControlNumber.Create(710),
            "LEADNB",
            "Lead Non-Blocking Qualification",
            isBlocking: false);
        leadRole.AddRequiredQualification(leadQual.CtrlNbr);

        var crew         = Crew.Create("REGULAR", WorkAreaCtrlNbr, "Crew B");
        var leadStaffPos = ControlNumber.Create(910);
        var apprStaffPos = ControlNumber.Create(911);
        var leadPosition = CrewPosition.Create(crew.CtrlNbr, leadRole.CtrlNbr, 1, leadStaffPos);
        var apprPosition = CrewPosition.Create(crew.CtrlNbr, apprenticeRole.CtrlNbr, 2, apprStaffPos);

        var incumbency = CrewIncumbency.Create(apprPosition.CtrlNbr, EmpJuniorUnqualified, DateTime.UtcNow.AddDays(-5));

        var board = RosterBoard.Create(CraftCtrlNbr, RosterCtrlNbr, "Extra Board", BoardType.ExtraBoard);
        board.AddPosition(EmpSeniorQualified, 1, ControlNumber.Create(960));

        var seniorities = new[]
        {
            MakeSeniority(EmpJuniorUnqualified, DateTime.UtcNow.AddDays(-10)),
            MakeSeniority(EmpSeniorQualified, DateTime.UtcNow.AddDays(-1000)),
        };

        var employeeQualifications = new[]
        {
            EmployeeQualification.Create(EmpSeniorQualified, leadQual.CtrlNbr, "test", achievedAtUtc: DateTime.UtcNow.AddDays(-200)),
        };

        var vacancy = PositionVacancy.Create(WorkAreaCtrlNbr, StaffablePositionType.Crew, leadStaffPos, CraftCtrlNbr, "VACANCY");
        var bulletin = Bulletin.Create(vacancy.CtrlNbr, CraftCtrlNbr, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        var rule = BulletinRule.Create(
            CraftCtrlNbr,
            48,
            new TimeSpan(8, 0, 0),
            new TimeSpan(17, 0, 0),
            5,
            new TimeSpan(8, 0, 0),
            8,
            ForceAssignSelectionMode.JuniorHelperOrExtraBoard);

        var uow = new ForceAssignFakeUow(
            bulletin: bulletin,
            rule: rule,
            vacancy: vacancy,
            craftRoles: [leadRole, apprenticeRole],
            crewPositionsByStaffPos: new() { [leadStaffPos] = leadPosition },
            crewPositionsByCrew: new() { [crew.CtrlNbr] = [leadPosition, apprPosition] },
            crewsByWorkArea: new() { [WorkAreaCtrlNbr] = [crew] },
            incumbenciesByPosition: new() { [apprPosition.CtrlNbr] = incumbency },
            boardsByCraft: [board],
            seniorities: seniorities,
            craftRoleQualifications: leadRole.RequiredQualifications.ToList(),
            qualificationTypes: [leadQual],
            employeeQualifications: employeeQualifications);

        var sut = BuildBulletins(uow);

        var candidate = await sut.GetForceAssignCandidateAsync(bulletin.CtrlNbr, TestContext.Current.CancellationToken);

        Assert.Equal(EmpSeniorQualified, candidate);
    }

    private static Seniority MakeSeniority(ControlNumber employeeCtrlNbr, DateTime rosterDate) =>
        Seniority.Create(
            RosterCtrlNbr, employeeCtrlNbr, lastActiveRoster: true,
            rosterDate: rosterDate, rank: 1, seniorityStateCtrlNbr: SeniorityStateCtrlNbr, canTrain: true);

    private static BulletinsService BuildBulletins(IOrchestrationUnitOfWork uow)
    {
        var factory = new FakeUowFactory(uow);
        var railroadResolver = new RailroadResolver();
        var notifications = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            railroadResolver,
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));
        var eligibility = new EmployeeEligibilityService(factory);
        return new BulletinsService(
            factory, NullLogger<BulletinsService>.Instance, new FakeBulletinScheduleSignal(), notifications, eligibility);
    }
}

file sealed class FakeUowFactory(IOrchestrationUnitOfWork uow) : IOrchestrationUnitOfWorkFactory
{
    public Task<IOrchestrationUnitOfWork> CreateAsync(OrchestrationUnitOfWorkOptions? options = null, CancellationToken ct = default)
        => Task.FromResult(uow);
}

file sealed class FakeBulletinScheduleSignal : IBulletinScheduleSignal
{
    public void Notify(DateTime eventUtc) { }
    public Task WaitAsync(CancellationToken ct) => Task.CompletedTask;
}

file abstract class FakeRepoBase<T> : IRepository<T> where T : Entity
{
    public virtual Task<List<T>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<T>());
    public virtual Task<List<T>> GetAllAsync(int page, int size, CancellationToken ct = default) => Task.FromResult(new List<T>());
    public virtual Task<T?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<T?>(null);
    public virtual Task<T?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<T?>(null);
    public virtual Task AddAsync(T entity, CancellationToken ct = default) => Task.CompletedTask;
    public virtual Task UpdateAsync(T entity, CancellationToken ct = default) => Task.CompletedTask;
    public virtual Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
    public virtual Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
    public virtual void Add(T entity) { }
    public virtual void Update(T entity) { }
    public virtual void Remove(T entity) { }
}

file sealed class FakeBulletinRepo(Bulletin bulletin) : FakeRepoBase<Bulletin>, IBulletinRepository
{
    public override Task<Bulletin?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => Task.FromResult(bulletin.CtrlNbr == ctrlNbr ? bulletin : null);
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

file sealed class FakeBulletinRuleRepo(BulletinRule rule) : FakeRepoBase<BulletinRule>, IBulletinRuleRepository
{
    public Task<BulletinRule?> GetByCraftAsync(ControlNumber craftCtrlNbr) => Task.FromResult<BulletinRule?>(rule);
}

file sealed class FakeVacancyRepo(PositionVacancy vacancy) : FakeRepoBase<PositionVacancy>, IPositionVacancyRepository
{
    public override Task<PositionVacancy?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => Task.FromResult(vacancy.CtrlNbr == ctrlNbr ? vacancy : null);
    public Task<List<PositionVacancy>> GetOpenAsync() => Task.FromResult(new List<PositionVacancy>());
    public Task<List<PositionVacancy>> GetOpenByRailroadAsync(ControlNumber r) => Task.FromResult(new List<PositionVacancy>());
    public Task<List<PositionVacancy>> GetByTargetAsync(string t, ControlNumber c) => Task.FromResult(new List<PositionVacancy>());
    public Task<List<PositionVacancy>> GetByCraftAsync(ControlNumber c) => Task.FromResult(new List<PositionVacancy>());
    public Task<List<PositionVacancy>> GetByWorkAreaAsync(ControlNumber w) => Task.FromResult(new List<PositionVacancy>());
    public Task<double> GetAverageDailyBoardVacanciesAsync(ControlNumber w, ControlNumber c, CancellationToken ct = default) => Task.FromResult(0.0);
}

file sealed class FakeCraftRoleRepo(IReadOnlyList<CraftRole> roles) : FakeRepoBase<CraftRole>, ICraftRoleRepository
{
    public override Task<CraftRole?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => Task.FromResult(roles.SingleOrDefault(r => r.CtrlNbr == ctrlNbr));
    public Task<List<CraftRole>> GetByCraftAsync(ControlNumber c) => Task.FromResult(roles.Where(r => r.CraftCtrlNbr == c).ToList());
    public Task<List<CraftRole>> GetByDepartmentAsync(ControlNumber d) => Task.FromResult(new List<CraftRole>());
    public Task<List<CraftRole>> GetByRailroadAsync(ControlNumber r) => Task.FromResult(new List<CraftRole>());
    public Task<CraftRole?> GetByCtrlNbrWithQualificationsAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => Task.FromResult(roles.SingleOrDefault(r => r.CtrlNbr == ctrlNbr));
}

file sealed class FakeRosterBoardRepo(IReadOnlyList<RosterBoard> boards) : FakeRepoBase<RosterBoard>, IRosterBoardRepository
{
    public Task<IReadOnlyList<RosterBoard>> GetActiveByWorkAreaAsync(ControlNumber w, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RosterBoard>>([]);
    public Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrAsync(ControlNumber c, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<RosterBoard>>(boards.Where(b => b.CraftCtrlNbr == c).ToList());
    public Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> cs, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RosterBoard>>([]);
    public Task<RosterBoard?> GetByPositionCtrlNbrAsync(ControlNumber p, CancellationToken ct = default) => Task.FromResult<RosterBoard?>(null);
    public Task<RosterBoard?> GetByStaffablePositionCtrlNbrAsync(ControlNumber s, CancellationToken ct = default) => Task.FromResult<RosterBoard?>(null);
}

file sealed class FakeCrewRepo(IReadOnlyDictionary<ControlNumber, List<Crew>> crewsByWorkArea) : FakeRepoBase<Crew>, ICrewRepository
{
    public Task<List<Crew>> GetByWorkAreaAsync(ControlNumber w)
        => Task.FromResult(crewsByWorkArea.TryGetValue(w, out var crews) ? crews : []);
    public Task<List<Crew>> GetByTypeAsync(string t) => Task.FromResult(new List<Crew>());
    public Task<List<Crew>> GetByRailroadAsync(ControlNumber r) => Task.FromResult(new List<Crew>());
    public Task<bool> ExistsByNameInWorkAreaAsync(ControlNumber w, string n, ControlNumber? ex = null) => Task.FromResult(false);
}

file sealed class FakeCrewPositionRepo(
    IReadOnlyDictionary<ControlNumber, CrewPosition> byStaffPos,
    IReadOnlyDictionary<ControlNumber, List<CrewPosition>> byCrew) : FakeRepoBase<CrewPosition>, ICrewPositionRepository
{
    public Task<List<CrewPosition>> GetByCrewAsync(ControlNumber c)
        => Task.FromResult(byCrew.TryGetValue(c, out var positions) ? positions : []);
    public Task<List<CrewPosition>> GetByCrewsAsync(IEnumerable<ControlNumber> cs) => Task.FromResult(new List<CrewPosition>());
    public Task<CrewPosition?> GetByStaffablePositionAsync(ControlNumber s)
        => Task.FromResult(byStaffPos.TryGetValue(s, out var position) ? position : null);
    public Task<List<ControlNumber>> GetVacantStaffablePositionCtrlNbrsAsync(CancellationToken ct = default) => Task.FromResult(new List<ControlNumber>());
}

file sealed class FakeCrewIncumbencyRepo(IReadOnlyDictionary<ControlNumber, CrewIncumbency> byPosition) : FakeRepoBase<CrewIncumbency>, ICrewIncumbencyRepository
{
    public Task<List<CrewIncumbency>> GetByCrewPositionAsync(ControlNumber crewPositionCtrlNbr) => Task.FromResult(new List<CrewIncumbency>());
    public Task<List<CrewIncumbency>> GetActiveByEmployeeAsync(ControlNumber employeeCtrlNbr, DateTime asOfUtc) => Task.FromResult(new List<CrewIncumbency>());
    public Task<CrewIncumbency?> GetActiveByPositionAsync(ControlNumber crewPositionCtrlNbr, DateTime asOfUtc)
        => Task.FromResult(byPosition.TryGetValue(crewPositionCtrlNbr, out var incumbency) ? incumbency : null);
}

file sealed class FakeSeniorityRepo(IReadOnlyList<Seniority> seniorities) : FakeRepoBase<Seniority>, ISeniorityRepository
{
    public Task<List<Seniority>> GetByRosterCtrlNbrAsync(ControlNumber rosterCtrlNbr) => Task.FromResult(new List<Seniority>());
    public Task<List<Seniority>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
        => Task.FromResult(seniorities.Where(s => s.EmployeeCtrlNbr == employeeCtrlNbr).ToList());
}

file sealed class FakeCraftRoleQualificationRepo(IReadOnlyList<CraftRoleQualification> quals) : FakeRepoBase<CraftRoleQualification>, ICraftRoleQualificationRepository
{
    public Task<List<CraftRoleQualification>> GetByCraftRoleAsync(ControlNumber craftRoleCtrlNbr)
        => Task.FromResult(quals.Where(q => q.CraftRoleCtrlNbr == craftRoleCtrlNbr).ToList());
}

file sealed class FakeQualificationTypeRepo(IReadOnlyList<QualificationType> types) : FakeRepoBase<QualificationType>, IQualificationTypeRepository
{
    public override Task<QualificationType?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
        => Task.FromResult(types.SingleOrDefault(t => t.CtrlNbr == ctrlNbr));
    public Task<List<QualificationType>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr) => Task.FromResult(new List<QualificationType>());
    public Task<QualificationType?> GetByCodeAsync(ControlNumber parentCtrlNbr, string code) => Task.FromResult<QualificationType?>(null);
    public Task<List<QualificationType>> GetActiveByParentCtrlNbrAsync(ControlNumber parentCtrlNbr) => Task.FromResult(new List<QualificationType>());
    public Task<List<QualificationType>> GetActiveByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr) => Task.FromResult(new List<QualificationType>());
}

file sealed class FakeEmployeeQualificationRepo(IReadOnlyList<EmployeeQualification> quals) : FakeRepoBase<EmployeeQualification>, IEmployeeQualificationRepository
{
    public Task<List<EmployeeQualification>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr) => Task.FromResult(new List<EmployeeQualification>());
    public Task<EmployeeQualification?> GetByEmployeeAndTypeAsync(ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr)
        => Task.FromResult(quals.FirstOrDefault(q => q.EmployeeCtrlNbr == employeeCtrlNbr && q.QualificationTypeCtrlNbr == qualificationTypeCtrlNbr));
    public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr) => Task.FromResult(new List<EmployeeQualification>());
    public Task<List<EmployeeQualification>> GetActiveByEmployeeCtrlNbrsAsync(IEnumerable<ControlNumber> employeeCtrlNbrs) => Task.FromResult(new List<EmployeeQualification>());
    public Task<List<EmployeeQualification>> GetExpiringBeforeAsync(DateTime cutoffUtc) => Task.FromResult(new List<EmployeeQualification>());
}

file sealed class FakeSuspensionRepo : FakeRepoBase<EmployeeQualificationSuspension>, IEmployeeQualificationSuspensionRepository
{
    public Task<EmployeeQualificationSuspension?> GetActiveByEmployeeAndTypeAsync(ControlNumber employeeCtrlNbr, ControlNumber qualificationTypeCtrlNbr, CancellationToken ct = default) => Task.FromResult<EmployeeQualificationSuspension?>(null);
    public Task<List<EmployeeQualificationSuspension>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) => Task.FromResult(new List<EmployeeQualificationSuspension>());
}

file sealed class ForceAssignFakeUow : IOrchestrationUnitOfWork
{
    private readonly FakeBulletinRepo _bulletins;
    private readonly FakeBulletinRuleRepo _bulletinRules;
    private readonly FakeVacancyRepo _vacancies;
    private readonly FakeCraftRoleRepo _craftRoles;
    private readonly FakeRosterBoardRepo _rosterBoards;
    private readonly FakeCrewRepo _crews;
    private readonly FakeCrewPositionRepo _crewPositions;
    private readonly FakeCrewIncumbencyRepo _incumbencies;
    private readonly FakeSeniorityRepo _seniority;
    private readonly FakeCraftRoleQualificationRepo _craftRoleQuals;
    private readonly FakeQualificationTypeRepo _qualificationTypes;
    private readonly FakeEmployeeQualificationRepo _employeeQuals;
    private readonly FakeSuspensionRepo _suspensions = new();

    public ForceAssignFakeUow(
        Bulletin bulletin,
        BulletinRule rule,
        PositionVacancy vacancy,
        IReadOnlyList<CraftRole> craftRoles,
        Dictionary<ControlNumber, CrewPosition> crewPositionsByStaffPos,
        Dictionary<ControlNumber, List<CrewPosition>> crewPositionsByCrew,
        Dictionary<ControlNumber, List<Crew>> crewsByWorkArea,
        Dictionary<ControlNumber, CrewIncumbency> incumbenciesByPosition,
        IReadOnlyList<RosterBoard> boardsByCraft,
        IReadOnlyList<Seniority> seniorities,
        IReadOnlyList<CraftRoleQualification> craftRoleQualifications,
        IReadOnlyList<QualificationType> qualificationTypes,
        IReadOnlyList<EmployeeQualification> employeeQualifications)
    {
        _bulletins = new FakeBulletinRepo(bulletin);
        _bulletinRules = new FakeBulletinRuleRepo(rule);
        _vacancies = new FakeVacancyRepo(vacancy);
        _craftRoles = new FakeCraftRoleRepo(craftRoles);
        _rosterBoards = new FakeRosterBoardRepo(boardsByCraft);
        _crews = new FakeCrewRepo(crewsByWorkArea);
        _crewPositions = new FakeCrewPositionRepo(crewPositionsByStaffPos, crewPositionsByCrew);
        _incumbencies = new FakeCrewIncumbencyRepo(incumbenciesByPosition);
        _seniority = new FakeSeniorityRepo(seniorities);
        _craftRoleQuals = new FakeCraftRoleQualificationRepo(craftRoleQualifications);
        _qualificationTypes = new FakeQualificationTypeRepo(qualificationTypes);
        _employeeQuals = new FakeEmployeeQualificationRepo(employeeQualifications);
    }

    public string CorrelationId => "test";
    public string OrchestrationId => "test";

    public IBulletinRepository Bulletins => _bulletins;
    public IBulletinRuleRepository BulletinRules => _bulletinRules;
    public IPositionVacancyRepository PositionVacancies => _vacancies;
    public ICraftRoleRepository CraftRoles => _craftRoles;
    public IRosterBoardRepository RosterBoards => _rosterBoards;
    public ICrewRepository Crews => _crews;
    public ICrewPositionRepository CrewPositions => _crewPositions;
    public ICrewIncumbencyRepository CrewIncumbencies => _incumbencies;
    public ISeniorityRepository Seniority => _seniority;
    public ICraftRoleQualificationRepository CraftRoleQualifications => _craftRoleQuals;
    public IQualificationTypeRepository QualificationTypes => _qualificationTypes;
    public IEmployeeQualificationRepository EmployeeQualifications => _employeeQuals;
    public IEmployeeQualificationSuspensionRepository QualificationSuspensions => _suspensions;

    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public void Dispose() { }
    public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateUserProfileAsync(string userId, string firstName, string? middleName, string lastName, string fullName, string fullNameLnf, string employeeNumber, CancellationToken ct = default) => Task.CompletedTask;

    // ── unused interface members (selection path does not touch these) ──
    public IBulletinBidRepository BulletinBids => throw new NotImplementedException();
    public IPositionAssignmentRepository PositionAssignments => throw new NotImplementedException();
    public IStaffablePositionRepository StaffablePositions => throw new NotImplementedException();
    public IDynamicGroupRepository DynamicGroups => throw new NotImplementedException();
    public IEmployeeNotificationRepository EmployeeNotifications => throw new NotImplementedException();
    public INotificationTypeConfigRepository NotificationTypeConfigs => throw new NotImplementedException();
    public IRosterRepository Rosters => throw new NotImplementedException();
    public ICrewAssignmentRepository CrewAssignments => throw new NotImplementedException();
    public ICrewAttachmentInstanceRepository CrewAttachmentInstances => throw new NotImplementedException();
    public IAssignmentRepository Assignments => throw new NotImplementedException();
    public IAssignmentScheduleRepository AssignmentSchedules => throw new NotImplementedException();
    public ISlotRequirementRepository SlotRequirements => throw new NotImplementedException();
    public IPositionSlotRepository PositionSlots => throw new NotImplementedException();
    public IWorkInstanceRepository WorkInstances => throw new NotImplementedException();
    public IShiftDefinitionRepository ShiftDefinitions => throw new NotImplementedException();
    public IShiftInstanceRepository ShiftInstances => throw new NotImplementedException();
    public IOnDutyRecordRepository OnDutyRecords => throw new NotImplementedException();
    public IOffDutyRecordRepository OffDutyRecords => throw new NotImplementedException();
    public IBoardCascadePolicyRepository BoardCascadePolicies => throw new NotImplementedException();
    public IRequiredPositionsStrategyRepository RequiredPositionsStrategies => throw new NotImplementedException();
    public ICraftRequiredPositionsStrategyRepository CraftRequiredPositionsStrategies => throw new NotImplementedException();
    public ICraftOperationsPolicyRepository CraftOperationsPolicies => throw new NotImplementedException();
    public ICraftDisplacementPolicyRepository CraftDisplacementPolicies => throw new NotImplementedException();
    public IDisplacementCaseRepository DisplacementCases => throw new NotImplementedException();
    public IDisplacementClaimRepository DisplacementClaims => throw new NotImplementedException();
    public IBulletinPolicyRepository BulletinPolicies => throw new NotImplementedException();
    public ICallSheetRuleRepository CallSheetRules => throw new NotImplementedException();
        public ICraftCallSheetRuleRepository CraftCallSheetRules => null!;
    public IDepartmentReassignmentRuleRepository DepartmentReassignmentRules => throw new NotImplementedException();
    public ISeniorityMovePolicyRepository SeniorityMovePolicies => throw new NotImplementedException();
    public ISeniorityMoveRepository SeniorityMoves => new NoOpSeniorityMoveRepository();
    public IEmployeeRepository Employees => throw new NotImplementedException();
    public IEmailAddressRepository EmailAddresses => throw new NotImplementedException();
    public IParentRepository Parents => throw new NotImplementedException();
    public IAddressTypeRepository AddressTypes => throw new NotImplementedException();
    public IPhoneNumberTypeRepository PhoneNumberTypes => throw new NotImplementedException();
    public IEmailAddressTypeRepository EmailAddressTypes => throw new NotImplementedException();
    public IEmploymentStatusRepository EmploymentStatuses => throw new NotImplementedException();
    public IEmploymentStatusHistoryRepository EmploymentStatusHistory => throw new NotImplementedException();
    public IEmployeePriorServiceCreditRepository EmployeePriorServiceCredits => throw new NotImplementedException();
    public ICraftRepository Crafts => throw new NotImplementedException();
    public ISeniorityStateRepository SeniorityStates => throw new NotImplementedException();
    public IGroupTypeRepository GroupTypes => throw new NotImplementedException();
    public IGroupAttributeDefinitionRepository AttributeDefinitions => throw new NotImplementedException();
    public IGroupAttributeValueRepository AttributeValues => throw new NotImplementedException();
    public IDepartmentRepository Departments => throw new NotImplementedException();
    public IQualificationRequirementRepository QualificationRequirements => throw new NotImplementedException();
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
    public IAbsenceRequestRepository AbsenceRequests => throw new NotImplementedException();
    public IVacancyImpactRepository VacancyImpacts => throw new NotImplementedException();
    public ISafetyObservationRepository SafetyObservations => throw new NotImplementedException();
    public ISafetyObservationResolutionRepository SafetyResolutions => throw new NotImplementedException();
    public ISafetyCategoryRepository SafetyCategories => throw new NotImplementedException();
    public IRailroadInformationRepository RailroadInformation => throw new NotImplementedException();
    public IRailroadInformationReadReceiptRepository RailroadInformationReadReceipts => throw new NotImplementedException();
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
    public IRoleRepository Roles => throw new NotImplementedException();
    public IFeatureRepository Features => throw new NotImplementedException();
    public IPermissionRepository Permissions => throw new NotImplementedException();
    public IUserParentAssignmentRepository UserParentAssignments => throw new NotImplementedException();
    public IInvitationRepository Invitations => throw new NotImplementedException();
    public IPayrollTierRepository PayrollTiers => throw new NotImplementedException();
    public ISeniorityStateVacancyConfigRepository SeniorityStateVacancyConfigs => throw new NotImplementedException();
    public IPendingSeniorityStateChangeRepository PendingSeniorityStateChanges => throw new NotImplementedException();
}
