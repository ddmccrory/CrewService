using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Boards;
using CrewService.Application.Bulletins;
using CrewService.Application.Crews;
using CrewService.Application.Notifications;
using CrewService.Application.Qualifications;
using CrewService.Application.RosterBoardOps;
using CrewService.Application.SeniorityOps;
using CrewService.Application.TenantConfig;
using CrewService.Application.VacancyAssignment;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Persistance.Data;
using CrewService.Persistance.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrewService.UnitTests.Fixtures;

/// <summary>
/// Integration test host for the seniority-state vacancy-action flow. Wires the real
/// <see cref="OrchestrationUnitOfWorkFactory"/> and the concrete application services it drives
/// over a single shared in-memory SQLite connection, mirroring one request scope. This is required
/// because the flow issues sequential units of work on the same connection, and the fix under test
/// depends on that transaction sequencing — a faked UoW would bypass the very path being verified.
/// </summary>
internal sealed class SeniorityVacancyTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CrewServiceDbContext _crewContext;
    private readonly UserAccessDbContext _userContext;

    public SeniorityVacancyTestHost()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var currentUser = new TestCurrentUserService();
        var encryptor = new TestFieldEncryptor();

        var crewOptions = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(_connection)
            .Options;
        _crewContext = new CrewServiceDbContext(crewOptions, currentUser, encryptor);
        _crewContext.Database.EnsureCreated();

        var userOptions = new DbContextOptionsBuilder<UserAccessDbContext>()
            .UseSqlite(_connection)
            .Options;
        _userContext = new UserAccessDbContext(userOptions);

        UowFactory = new OrchestrationUnitOfWorkFactory(
            _connection, _crewContext, _userContext, currentUser, NullLoggerFactory.Instance);

        var scheduleSignal = new BulletinScheduleSignal();
        var railroadResolver = new RailroadResolver();
        var notifications = new EmployeeNotificationService(NullLogger<EmployeeNotificationService>.Instance, railroadResolver);
        var eligibility = new EmployeeEligibilityService(UowFactory);
        Bulletins = new BulletinsService(
            UowFactory, NullLogger<BulletinsService>.Instance, scheduleSignal, notifications, eligibility);
        Repost = new VacancyRepostService(UowFactory, Bulletins, NullLogger<VacancyRepostService>.Instance);

        Crews = new CrewsAppService(UowFactory, Repost, NullLogger<CrewsAppService>.Instance);
        RosterBoards = new RosterBoardAppService(
            UowFactory,
            new RequiredPositionsFormulaRegistry([new StaticFormula(), new AnnualizedAverageFormula()]),
            Repost,
            notifications);
        VacancyConfig = new SeniorityStateVacancyConfigService(
            UowFactory, Crews, RosterBoards, railroadResolver, NullLogger<SeniorityStateVacancyConfigService>.Instance);
        Seniority = new SeniorityAppService(UowFactory, new QualificationReactiveService(), VacancyConfig);
    }

    public IOrchestrationUnitOfWorkFactory UowFactory { get; }
    public CrewsAppService Crews { get; }
    public RosterBoardAppService RosterBoards { get; }
    public SeniorityStateVacancyConfigService VacancyConfig { get; }
    public SeniorityAppService Seniority { get; }
    public BulletinsService Bulletins { get; }
    public VacancyRepostService Repost { get; }

    /// <summary>
    /// Creates a fresh <see cref="CrewServiceDbContext"/> on the shared connection for seeding and
    /// asserting outside the orchestration UoWs. A new context avoids reading stale change-tracked
    /// state after the services commit.
    /// </summary>
    public CrewServiceDbContext CreateReadContext()
    {
        var options = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new CrewServiceDbContext(options, new TestCurrentUserService(), new TestFieldEncryptor());
    }

    public void Dispose()
    {
        _crewContext.Dispose();
        _userContext.Dispose();
        _connection.Dispose();
    }
}
