namespace CrewService.Application.Workflows;

public sealed class WorkflowTriggerExecutionException(
    IReadOnlyList<WorkflowExecutionStepOutcomeRecord> stepOutcomes,
    Exception innerException) : Exception(innerException.Message, innerException)
{
    public IReadOnlyList<WorkflowExecutionStepOutcomeRecord> StepOutcomes { get; } = stepOutcomes;
}
