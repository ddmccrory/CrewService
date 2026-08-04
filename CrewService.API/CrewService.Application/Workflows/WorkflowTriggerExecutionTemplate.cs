using CrewService.Application.Workflows.Effects;
using CrewService.Domain.Modules.Workflows;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Workflows;

public sealed class WorkflowTriggerExecutionTemplate(
    IWorkflowEffectRunner workflowEffectRunner,
    ILogger<WorkflowTriggerExecutionTemplate> logger) : IWorkflowTriggerExecutionTemplate
{
    public async Task<WorkflowTriggerExecutionResult> ExecuteAsync(WorkflowTriggerExecutionContext context)
    {
        var postCommitWorkItems = new List<WorkflowEffectPostCommitWorkItem>();
        var stepOutcomes = new List<WorkflowExecutionStepOutcomeRecord>();

        foreach (var step in context.Definition.Steps.OrderBy(s => s.Order))
        {
            var effectOutcomes = new List<WorkflowExecutionEffectOutcomeRecord>();
            var stepStatus = WorkflowExecutionStatus.Succeeded;
            string? stepMessage = null;

            if (!step.IsEnabled)
            {
                stepStatus = WorkflowExecutionStatus.Skipped;
                stepMessage = "Step disabled.";
                stepOutcomes.Add(new WorkflowExecutionStepOutcomeRecord(
                    step.Order,
                    step.Name,
                    stepStatus,
                    stepMessage,
                    effectOutcomes));
                continue;
            }

            if (!ShouldExecuteConditions(step.ConditionGroupOperator, step.Conditions, context.Metadata, context.ReferenceData))
            {
                stepStatus = WorkflowExecutionStatus.Skipped;
                stepMessage = "Step conditions did not match.";
                stepOutcomes.Add(new WorkflowExecutionStepOutcomeRecord(
                    step.Order,
                    step.Name,
                    stepStatus,
                    stepMessage,
                    effectOutcomes));
                continue;
            }

            foreach (var effect in step.Effects.OrderBy(e => e.Order))
            {
                var effectType = ResolveEffectTypeName(effect.EffectTypeCtrlNbr, context.ReferenceData);
                var effectOutcome = new WorkflowExecutionEffectOutcomeRecord(
                    effect.Order,
                    effect.EffectTypeCtrlNbr.Value,
                    effectType,
                    BuildEffectOptions(effect),
                    WorkflowExecutionStatus.Succeeded,
                    null);

                if (!effect.IsEnabled)
                {
                    effectOutcomes.Add(effectOutcome with
                    {
                        Status = WorkflowExecutionStatus.Skipped,
                        Message = "Effect disabled."
                    });
                    continue;
                }

                try
                {
                    var workItems = await workflowEffectRunner.ExecuteDatabaseEffectAsync(
                        context.Uow,
                        effect,
                        new WorkflowRuntimeEffectReferenceData(
                            context.ReferenceData.EffectTypeCodeByCtrlNbr,
                            context.ReferenceData.OperatorCodeByCtrlNbr,
                            context.ReferenceData.MetadataFieldCodeByCtrlNbr),
                        context.EffectRuntimeContext,
                        context.CancellationToken);

                    if (workItems.Count > 0)
                        postCommitWorkItems.AddRange(workItems);

                    effectOutcomes.Add(effectOutcome);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Workflow trigger effect failed. TriggerType: {TriggerType}, StepOrder: {StepOrder}, EffectType: {EffectType}",
                        context.TriggerTypeCtrlNbr.Value,
                        step.Order,
                        effect.EffectTypeCtrlNbr.Value);

                    effectOutcomes.Add(effectOutcome with
                    {
                        Status = WorkflowExecutionStatus.Failed,
                        Message = ex.Message
                    });

                    stepStatus = WorkflowExecutionStatus.Failed;

                    if (string.Equals(step.FailurePolicy, WorkflowFailurePolicies.StopWorkflow, StringComparison.OrdinalIgnoreCase))
                    {
                        stepOutcomes.Add(new WorkflowExecutionStepOutcomeRecord(
                            step.Order,
                            step.Name,
                            stepStatus,
                            stepMessage,
                            effectOutcomes));

                        throw new WorkflowTriggerExecutionException(stepOutcomes, ex);
                    }
                }
            }

            stepOutcomes.Add(new WorkflowExecutionStepOutcomeRecord(
                step.Order,
                step.Name,
                stepStatus,
                stepMessage,
                effectOutcomes));
        }

        return new WorkflowTriggerExecutionResult(
            WorkflowExecutionStatus.Succeeded,
            stepOutcomes,
            postCommitWorkItems);
    }

    private static bool ShouldExecuteConditions(
        string conditionGroupOperator,
        IReadOnlyList<WorkflowConditionDefinition> conditions,
        WorkflowRuntimeTriggerMetadata metadata,
        WorkflowRuntimeTriggerReferenceData referenceData)
    {
        if (conditions.Count == 0)
            return true;

        var useAll = !string.Equals(conditionGroupOperator, "ANY", StringComparison.OrdinalIgnoreCase);

        return useAll
            ? conditions.All(c => EvaluateCondition(c, metadata, referenceData))
            : conditions.Any(c => EvaluateCondition(c, metadata, referenceData));
    }

    private static bool EvaluateCondition(
        WorkflowConditionDefinition condition,
        WorkflowRuntimeTriggerMetadata metadata,
        WorkflowRuntimeTriggerReferenceData referenceData)
    {
        if (!referenceData.MetadataFieldCodeByCtrlNbr.TryGetValue(condition.FieldTypeCtrlNbr, out var fieldCode)
            || !metadata.ValuesByFieldCode.TryGetValue(fieldCode, out var actualValue)
            || string.IsNullOrWhiteSpace(actualValue))
            return false;

        if (!referenceData.OperatorCodeByCtrlNbr.TryGetValue(condition.OperatorTypeCtrlNbr, out var operatorCode))
            throw new InvalidOperationException($"Unsupported workflow operator control number '{condition.OperatorTypeCtrlNbr.Value}'.");

        return operatorCode switch
        {
            WorkflowOperatorTypeCodes.EqualsOperator => string.Equals(actualValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            WorkflowOperatorTypeCodes.NotEquals => !string.Equals(actualValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            _ => throw new InvalidOperationException($"Unsupported workflow operator '{operatorCode}'.")
        };
    }

    private static string ResolveEffectTypeName(
        Domain.ValueObjects.ControlNumber effectTypeCtrlNbr,
        WorkflowRuntimeTriggerReferenceData referenceData)
    {
        return referenceData.EffectTypeCodeByCtrlNbr.TryGetValue(effectTypeCtrlNbr, out var effectTypeName)
            ? effectTypeName
            : effectTypeCtrlNbr.Value.ToString();
    }

    private static Dictionary<string, string> BuildEffectOptions(WorkflowEffectDefinition effect)
    {
        return effect.Options.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }
}
