using CrewService.Application.Employees;
using CrewService.Application.TenantConfig;
using CrewService.Application.VacancyAssignment;
using CrewService.Application.Workflows;
using CrewService.Application.Workflows.Effects;
using CrewService.Domain.DomainEvents.Employees;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrewService.UnitTests.Workflows;

public sealed class EmployeeReactiveServiceTests
{
    [Fact]
    public async Task HandleEmployeeCreatedAsync_WithMissingPayload_DoesNotThrow()
    {
        var effectRunner = new WorkflowEffectRunner(
            new WorkflowEffectHandlerFactory([]),
            new WorkflowEffectExecutionTemplate(new NoOpWorkflowEffectExecutionGuard()));
        var triggerTemplate = new WorkflowTriggerExecutionTemplate(
            effectRunner,
            NullLogger<WorkflowTriggerExecutionTemplate>.Instance);
        var runtime = new WorkflowRuntimeService(
            uowFactory: new ThrowingUowFactory(),
            workflowTriggerExecutionTemplate: triggerTemplate,
            workflowPostCommitDispatcher: new NoOpWorkflowPostCommitDispatcher(),
            railroadResolver: new NoOpRailroadResolver(),
            logger: NullLogger<WorkflowRuntimeService>.Instance);

        var sut = new EmployeeReactiveService(runtime, NullLogger<EmployeeReactiveService>.Instance);

        var evt = new EmployeeCreatedDomainEvent(
            aggregateCtrlNbr: ControlNumber.Create(10),
            clientCtrlNbr: ControlNumber.Create(20),
            railroadCtrlNbr: ControlNumber.Create(30),
            email: "x@example.com",
            invitedByUserId: "u",
            invitedByUserName: "n",
            parentName: "p")
        {
            PayloadJson = null
        };

        await sut.HandleEmployeeCreatedAsync(evt, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_WhenRuntimeThrows_PropagatesException()
    {
        var effectRunner = new WorkflowEffectRunner(
            new WorkflowEffectHandlerFactory([]),
            new WorkflowEffectExecutionTemplate(new NoOpWorkflowEffectExecutionGuard()));
        var triggerTemplate = new WorkflowTriggerExecutionTemplate(
            effectRunner,
            NullLogger<WorkflowTriggerExecutionTemplate>.Instance);
        var runtime = new WorkflowRuntimeService(
            uowFactory: new ThrowingUowFactory(),
            workflowTriggerExecutionTemplate: triggerTemplate,
            workflowPostCommitDispatcher: new NoOpWorkflowPostCommitDispatcher(),
            railroadResolver: new NoOpRailroadResolver(),
            logger: NullLogger<WorkflowRuntimeService>.Instance);

        var sut = new EmployeeReactiveService(runtime, NullLogger<EmployeeReactiveService>.Instance);

        var evt = new EmployeeCreatedDomainEvent(
            aggregateCtrlNbr: ControlNumber.Create(10),
            clientCtrlNbr: ControlNumber.Create(20),
            railroadCtrlNbr: ControlNumber.Create(30),
            email: "x@example.com",
            invitedByUserId: "u",
            invitedByUserName: "n",
            parentName: "p");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleEmployeeCreatedAsync(evt, TestContext.Current.CancellationToken));
    }

    private sealed class ThrowingUowFactory : IOrchestrationUnitOfWorkFactory
    {
        public Task<IOrchestrationUnitOfWork> CreateAsync(OrchestrationUnitOfWorkOptions? options = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("runtime failed");
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

    private sealed class NoOpWorkflowPostCommitDispatcher : IWorkflowPostCommitDispatcher
    {
        public Task DispatchAsync(IReadOnlyList<WorkflowEffectPostCommitWorkItem> workItems, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpRailroadResolver : IRailroadResolver
    {
        public Task<ControlNumber?> ResolveFromWorkAreaAsync(
            IOrchestrationUnitOfWork uow,
            ControlNumber workAreaGroupCtrlNbr,
            CancellationToken ct = default)
            => Task.FromResult<ControlNumber?>(null);

        public ControlNumber? ResolveFromGroup(Domain.Modules.TenantConfig.DynamicGroup? group)
            => null;
    }

    private sealed class NoOpVacancyRepostService : IVacancyRepostService
    {
        public Task RepostVacatedPositionAsync(ControlNumber staffablePositionCtrlNbr, ControlNumber? previousIncumbentCtrlNbr = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task RepostBoardPositionIfUnderstaffedAsync(ControlNumber boardCtrlNbr, ControlNumber vacatedStaffablePositionCtrlNbr, ControlNumber? previousIncumbentCtrlNbr = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<int> ReconcileUnbulletinedVacantPositionsAsync(CancellationToken ct = default)
            => Task.FromResult(0);
    }

    private sealed class NoOpWorkflowVersionRepository : IWorkflowVersionRepository
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