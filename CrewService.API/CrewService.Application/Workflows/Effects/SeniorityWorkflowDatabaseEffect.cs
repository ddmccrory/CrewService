using CrewService.Domain.Modules.Workflows;

namespace CrewService.Application.Workflows.Effects;

public sealed class SeniorityWorkflowDatabaseEffect(
    SeniorityWorkflowAssignmentPath assignmentPath) : IDatabaseWorkflowEffect
{
    public string EffectTypeCode => WorkflowEffectTypeCodes.VacatePositionAndBulletinPosition;

    public async Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteAsync(WorkflowEffectExecutionContext context)
    {
        var runtime = context.RuntimeContext;
        if (runtime.EmployeeCtrlNbr is null)
            return [];

        var vacateResults = await assignmentPath.VacateEmployeeAssignmentsAsync(
            context.Uow,
            runtime.EmployeeCtrlNbr,
            context.CancellationToken);

        return SeniorityWorkflowPostCommitWorkBuilder.BuildVacancyRepostWorkItems(vacateResults);
    }
}