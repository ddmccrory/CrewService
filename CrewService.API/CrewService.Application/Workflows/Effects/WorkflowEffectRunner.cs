using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Workflows.Effects;

public sealed class WorkflowEffectRunner(
    IWorkflowEffectHandlerFactory workflowEffectHandlerFactory,
    IWorkflowEffectExecutionTemplate workflowEffectExecutionTemplate) : IWorkflowEffectRunner
{
    public async Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteDatabaseEffectAsync(
        IOrchestrationUnitOfWork uow,
        WorkflowEffectDefinition effect,
        WorkflowRuntimeEffectReferenceData referenceData,
        WorkflowEffectRuntimeContext runtimeContext,
        CancellationToken ct = default)
    {
        if (!referenceData.EffectTypeCodeByCtrlNbr.TryGetValue(effect.EffectTypeCtrlNbr, out var effectTypeCode))
            throw new InvalidOperationException($"Unsupported workflow effect type '{effect.EffectTypeCtrlNbr.Value}'.");

        var handler = workflowEffectHandlerFactory.Resolve(effectTypeCode);

        var context = new WorkflowEffectExecutionContext(
            uow,
            effect,
            effectTypeCode,
            referenceData,
            runtimeContext,
            ct);

        return await workflowEffectExecutionTemplate.ExecuteAsync(handler, context);
    }
}
