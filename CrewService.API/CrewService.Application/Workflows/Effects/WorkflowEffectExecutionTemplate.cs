using CrewService.Domain.Interfaces;

namespace CrewService.Application.Workflows.Effects;

public sealed class WorkflowEffectExecutionTemplate(IWorkflowEffectExecutionGuard workflowEffectExecutionGuard) : IWorkflowEffectExecutionTemplate
{
    public async Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteAsync(
        IDatabaseWorkflowEffect handler,
        WorkflowEffectExecutionContext context)
    {
        using var scope = workflowEffectExecutionGuard.BeginWorkflowDbEffectExecutionScope();
        return await handler.ExecuteAsync(context);
    }
}
