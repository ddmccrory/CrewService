using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Workflows.Effects;

public sealed record WorkflowEffectExecutionContext(
    IOrchestrationUnitOfWork Uow,
    WorkflowEffectDefinition Effect,
    string EffectTypeCode,
    WorkflowRuntimeEffectReferenceData ReferenceData,
    WorkflowEffectRuntimeContext RuntimeContext,
    CancellationToken CancellationToken);

public sealed record WorkflowEffectRuntimeContext(
    string? PrimaryEmail,
    long ClientCtrlNbr,
    string TriggerEmail,
    string InvitedByUserId,
    string InvitedByUserName,
    ControlNumber TriggerRailroadCtrlNbr,
    ControlNumber? EmployeeCtrlNbr,
    ControlNumber? RosterCtrlNbr,
    ControlNumber? SeniorityStateCtrlNbr,
    WorkflowPlaceOnDutyRuntimePayload? PlaceOnDutyPayload = null,
    WorkflowPositionVacatedRuntimePayload? PositionVacatedPayload = null);

public sealed record WorkflowPlaceOnDutyRuntimePayload(
    ControlNumber PositionSlotCtrlNbr,
    ControlNumber EmployeeCtrlNbr,
    DateTime OnDutyTimeUtc,
    DateTime ScheduledOnDutyTimeUtc,
    bool IsAssigned,
    int LateCallThresholdMinutes = 0);

public sealed record WorkflowPositionVacatedRuntimePayload(
    ControlNumber StaffablePositionCtrlNbr,
    ControlNumber CraftCtrlNbr,
    string PositionTypeCode,
    string VacancyReasonCode,
    ControlNumber? PreviousIncumbentCtrlNbr,
    ControlNumber? BoardCtrlNbr = null,
    ControlNumber? RosterCtrlNbr = null);

public sealed record WorkflowEffectPostCommitWorkItem(
    string WorkType,
    object Payload);

public interface IDatabaseWorkflowEffect
{
    string EffectTypeCode { get; }

    Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteAsync(WorkflowEffectExecutionContext context);
}

public interface IWorkflowPostCommitDispatcher
{
    Task DispatchAsync(IReadOnlyList<WorkflowEffectPostCommitWorkItem> workItems, CancellationToken ct = default);
}

public interface IWorkflowEffectRunner
{
    Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteDatabaseEffectAsync(
        IOrchestrationUnitOfWork uow,
        WorkflowEffectDefinition effect,
        WorkflowRuntimeEffectReferenceData referenceData,
        WorkflowEffectRuntimeContext runtimeContext,
        CancellationToken ct = default);
}

public interface IWorkflowEffectHandlerFactory
{
    IDatabaseWorkflowEffect Resolve(string effectTypeCode);
}

public interface IWorkflowEffectExecutionTemplate
{
    Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteAsync(
        IDatabaseWorkflowEffect handler,
        WorkflowEffectExecutionContext context);
}

public sealed record WorkflowRuntimeEffectReferenceData(
    IReadOnlyDictionary<ControlNumber, string> EffectTypeCodeByCtrlNbr,
    IReadOnlyDictionary<ControlNumber, string> OperatorCodeByCtrlNbr,
    IReadOnlyDictionary<ControlNumber, string> MetadataFieldCodeByCtrlNbr);
