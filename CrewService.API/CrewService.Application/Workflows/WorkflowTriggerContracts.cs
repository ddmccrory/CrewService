using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using CrewService.Application.Workflows.Effects;

namespace CrewService.Application.Workflows;

public sealed record WorkflowTriggerExecutionContext(
    IOrchestrationUnitOfWork Uow,
    WorkflowDefinition Definition,
    WorkflowVersion PublishedVersion,
    ControlNumber TriggerTypeCtrlNbr,
    ControlNumber TriggerRailroadCtrlNbr,
    ControlNumber AggregateCtrlNbr,
    WorkflowRuntimeTriggerReferenceData ReferenceData,
    WorkflowRuntimeTriggerMetadata Metadata,
    WorkflowEffectRuntimeContext EffectRuntimeContext,
    string? CorrelationId,
    CancellationToken CancellationToken);

public sealed record WorkflowRuntimeTriggerReferenceData(
    IReadOnlyDictionary<ControlNumber, string> EffectTypeCodeByCtrlNbr,
    IReadOnlyDictionary<ControlNumber, string> OperatorCodeByCtrlNbr,
    IReadOnlyDictionary<ControlNumber, string> MetadataFieldCodeByCtrlNbr);

public sealed record WorkflowRuntimeTriggerMetadata(
    IReadOnlyDictionary<string, string> ValuesByFieldCode);

public sealed record WorkflowTriggerExecutionResult(
    string Status,
    IReadOnlyList<WorkflowExecutionStepOutcomeRecord> StepOutcomes,
    IReadOnlyList<WorkflowEffectPostCommitWorkItem> PostCommitWorkItems);

public sealed record WorkflowExecutionStepOutcomeRecord(
    int StepOrder,
    string StepName,
    string Status,
    string? Message,
    IReadOnlyList<WorkflowExecutionEffectOutcomeRecord> Effects);

public sealed record WorkflowExecutionEffectOutcomeRecord(
    int EffectOrder,
    long EffectTypeCtrlNbr,
    string EffectType,
    IReadOnlyDictionary<string, string> Options,
    string Status,
    string? Message);

public interface IWorkflowTriggerExecutionTemplate
{
    Task<WorkflowTriggerExecutionResult> ExecuteAsync(WorkflowTriggerExecutionContext context);
}
