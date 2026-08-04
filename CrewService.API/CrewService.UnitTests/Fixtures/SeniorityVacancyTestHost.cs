using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Boards;
using CrewService.Application.Bulletins;
using CrewService.Application.Crews;
using CrewService.Application.Notifications;
using CrewService.Application.Policies;
using CrewService.Application.Qualifications;
using CrewService.Application.RosterBoardOps;
using CrewService.Application.SeniorityOps;
using CrewService.Application.TenantConfig;
using CrewService.Application.VacancyAssignment;
using CrewService.Application.Workflows;
using CrewService.Application.Workflows.Effects;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Repositories;
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
        SeedWorkflowReferenceData();

        var userOptions = new DbContextOptionsBuilder<UserAccessDbContext>()
            .UseSqlite(_connection)
            .Options;
        _userContext = new UserAccessDbContext(userOptions);

        UowFactory = new OrchestrationUnitOfWorkFactory(
            _connection,
            currentUser,
            encryptor,
            NullLoggerFactory.Instance);

        var scheduleSignal = new BulletinScheduleSignal();
        var railroadResolver = new RailroadResolver();
        var notifications = new EmployeeNotificationService(
            NullLogger<EmployeeNotificationService>.Instance,
            railroadResolver,
            new NotificationTypeConfigResolver(NullLogger<NotificationTypeConfigResolver>.Instance));
        var vacancySync = TestCallSheetVacancyProjectionSyncFactory.Create(UowFactory);
        var requirementEvaluation = new RequirementEvaluationService(UowFactory, []);
        var eligibility = new EmployeeEligibilityService(UowFactory);
        Bulletins = new BulletinsService(
            UowFactory, NullLogger<BulletinsService>.Instance, scheduleSignal, notifications, eligibility, vacancySync);
        Repost = new VacancyRepostService(UowFactory, Bulletins, NullLogger<VacancyRepostService>.Instance);
        var departmentReassignment = new DepartmentReassignmentService(vacancySync);

        Crews = new CrewsAppService(UowFactory, Repost, departmentReassignment, vacancySync, NullLogger<CrewsAppService>.Instance);
        RosterBoards = new RosterBoardAppService(
            UowFactory,
            requirementEvaluation,
            new RequiredPositionsFormulaRegistry([new StaticFormula(), new AnnualizedAverageFormula()]),
            Repost,
            departmentReassignment,
            notifications,
            vacancySync);

        var effectRunner = new WorkflowEffectRunner(
            new WorkflowEffectHandlerFactory([
                new SeniorityWorkflowDatabaseEffect(new SeniorityWorkflowAssignmentPath(Crews, RosterBoards)),
                new AddToRosterBoardWorkflowDatabaseEffect(
                    new SeniorityWorkflowAssignmentPath(Crews, RosterBoards),
                    RosterBoards,
                    NullLogger<AddToRosterBoardWorkflowDatabaseEffect>.Instance)
            ]),
            new WorkflowEffectExecutionTemplate(new NoOpWorkflowEffectExecutionGuard()));
        var triggerTemplate = new WorkflowTriggerExecutionTemplate(
            effectRunner,
            NullLogger<WorkflowTriggerExecutionTemplate>.Instance);

        var workflowRuntime = new WorkflowRuntimeService(
            uowFactory: UowFactory,
            workflowTriggerExecutionTemplate: triggerTemplate,
            workflowPostCommitDispatcher: new SeniorityOnlyWorkflowPostCommitDispatcher(Repost),
            railroadResolver: railroadResolver,
            logger: NullLogger<WorkflowRuntimeService>.Instance);
        var seniorityStateChangeSignal = new SeniorityStateChangeSignal();
        Seniority = new SeniorityAppService(
            UowFactory,
            requirementEvaluation,
            new QualificationReactiveService(),
            workflowRuntime,
            seniorityStateChangeSignal);
    }

    private void SeedWorkflowReferenceData()
    {
        if (!_crewContext.Set<WorkflowTriggerType>().Any())
        {
            _crewContext.Set<WorkflowTriggerType>().AddRange(
                WorkflowTriggerType.Create(WorkflowTriggerTypeCodes.EmployeeCreated, TriggerTypes.EmployeeCreated),
                WorkflowTriggerType.Create(WorkflowTriggerTypeCodes.SeniorityStatusChanged, TriggerTypes.SeniorityStateChanged));
        }

        if (!_crewContext.Set<WorkflowEffectType>().Any())
        {
            _crewContext.Set<WorkflowEffectType>().AddRange(
                WorkflowEffectType.Create(WorkflowEffectTypeCodes.SendInvitation, WorkflowEffectTypes.SendInvitation),
                WorkflowEffectType.Create(WorkflowEffectTypeCodes.DoNothing, WorkflowEffectTypes.DoNothing),
                WorkflowEffectType.Create(WorkflowEffectTypeCodes.AddToRosterBoard, WorkflowEffectTypes.AddToRosterBoard),
                WorkflowEffectType.Create(WorkflowEffectTypeCodes.VacatePositionAndBulletinPosition, WorkflowEffectTypes.VacatePositionAndBulletinPosition));
        }

        if (!_crewContext.Set<WorkflowOperatorType>().Any())
        {
            _crewContext.Set<WorkflowOperatorType>().AddRange(
                WorkflowOperatorType.Create(WorkflowOperatorTypeCodes.EqualsOperator, "Equals"),
                WorkflowOperatorType.Create(WorkflowOperatorTypeCodes.NotEquals, "Does Not Equal"));
        }

        if (!_crewContext.Set<WorkflowMetadataFieldType>().Any())
        {
            _crewContext.Set<WorkflowMetadataFieldType>().AddRange(
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.NewSeniorityState, "New Seniority State"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.DepartmentCtrlNbr, "Department CtrlNbr"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.DepartmentName, "Department Name"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.CraftCtrlNbr, "Craft CtrlNbr"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.CraftName, "Craft Name"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.SeniorityStateCtrlNbr, "Seniority State CtrlNbr"),
                WorkflowMetadataFieldType.Create(WorkflowMetadataFieldTypeCodes.SeniorityStateName, "Seniority State Name"));
        }

        _crewContext.SaveChanges();
    }

    public IOrchestrationUnitOfWorkFactory UowFactory { get; }
    public CrewsAppService Crews { get; }
    public RosterBoardAppService RosterBoards { get; }
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

    private sealed class NoOpWorkflowEffectExecutionGuard : IWorkflowEffectExecutionGuard
    {
        public bool IsInWorkflowDbEffectExecution => false;

        public IDisposable BeginWorkflowDbEffectExecutionScope() => NoOpScope.Instance;

        private sealed class NoOpScope : IDisposable
        {
            internal static readonly NoOpScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class SeniorityOnlyWorkflowPostCommitDispatcher(IVacancyRepostService repostService) : IWorkflowPostCommitDispatcher
    {
        public async Task DispatchAsync(IReadOnlyList<WorkflowEffectPostCommitWorkItem> workItems, CancellationToken ct = default)
        {
            foreach (var workItem in workItems)
            {
                switch (workItem.WorkType)
                {
                    case WorkflowPostCommitWorkTypes.RepostVacatedPosition:
                    {
                        if (workItem.Payload is not RepostVacatedPositionPostCommitPayload payload)
                            throw new InvalidOperationException("Invalid payload for RepostVacatedPosition.");

                        await repostService.RepostVacatedPositionAsync(
                            payload.StaffablePositionCtrlNbr,
                            payload.PreviousIncumbentCtrlNbr,
                            ct);
                        break;
                    }

                    case WorkflowPostCommitWorkTypes.RepostBoardPositionIfUnderstaffed:
                    {
                        if (workItem.Payload is not RepostBoardPositionIfUnderstaffedPostCommitPayload payload)
                            throw new InvalidOperationException("Invalid payload for RepostBoardPositionIfUnderstaffed.");

                        await repostService.RepostBoardPositionIfUnderstaffedAsync(
                            payload.BoardCtrlNbr,
                            payload.VacatedStaffablePositionCtrlNbr,
                            payload.PreviousIncumbentCtrlNbr,
                            ct);
                        break;
                    }
                }
            }
        }
    }

    private sealed class FakeWorkflowVersionRepository : IWorkflowVersionRepository
    {
        public Task<List<WorkflowVersion>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<WorkflowVersion>());
        public Task<List<WorkflowVersion>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default) => Task.FromResult(new List<WorkflowVersion>());
        public Task<WorkflowVersion?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<WorkflowVersion?>(null);
        public Task<WorkflowVersion?> GetByCtrlNbrIncludingDeletedAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.FromResult<WorkflowVersion?>(null);
        public Task AddAsync(WorkflowVersion entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(WorkflowVersion entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public Task RestoreAsync(ControlNumber ctrlNbr, CancellationToken ct = default) => Task.CompletedTask;
        public void Add(WorkflowVersion entity) { }
        public void Update(WorkflowVersion entity) { }
        public void Remove(WorkflowVersion entity) { }
        public Task<List<WorkflowVersion>> GetByTemplateAsync(ControlNumber workflowTemplateCtrlNbr, CancellationToken ct = default) => Task.FromResult(new List<WorkflowVersion>());
        public Task<WorkflowVersion?> GetLatestPublishedByRailroadAndTriggerAsync(ControlNumber railroadCtrlNbr, ControlNumber triggerTypeCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<WorkflowVersion?>(null);
    }
}
