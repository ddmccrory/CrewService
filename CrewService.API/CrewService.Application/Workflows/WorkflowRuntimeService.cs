using System.Text.Json;
using CrewService.Application.Crews;
using CrewService.Application.RosterBoardOps;
using CrewService.Application.SeniorityOps;
using CrewService.Application.TenantConfig;
using CrewService.Application.Workflows.Effects;
using CrewService.Domain.DomainEvents;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Workflows;

public sealed class WorkflowRuntimeService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IWorkflowTriggerExecutionTemplate workflowTriggerExecutionTemplate,
    IWorkflowPostCommitDispatcher workflowPostCommitDispatcher,
    IRailroadResolver railroadResolver,
    ILogger<WorkflowRuntimeService> logger)
{
    public async Task ExecuteEmployeeCreatedAsync(DomainEvent domainEvent, CancellationToken ct = default)
    {
        var payload = ParsePayload(domainEvent);
        var employeeCtrlNbr = ControlNumber.Create(domainEvent.AggregateId);
        IReadOnlyList<WorkflowEffectPostCommitWorkItem> postCommitWorkItems;

        {
            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

            var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct)
                ?? throw new InvalidOperationException($"Employee {employeeCtrlNbr.Value} was not found for workflow trigger.");

            var primaryEmail = employee.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.Email;

            var metadata = await BuildEmployeeMetadataAsync(uow, employeeCtrlNbr, ct);
            var railroadCtrlNbr = ResolveRailroadCtrlNbr(payload.RailroadCtrlNbr, metadata);
            var referenceData = await LoadReferenceDataAsync(uow, ct);

            if (!referenceData.TriggerTypeCtrlNbrByCode.TryGetValue(WorkflowTriggerTypeCodes.EmployeeCreated, out var employeeCreatedTriggerTypeCtrlNbr))
                throw new InvalidOperationException($"Workflow trigger type '{WorkflowTriggerTypeCodes.EmployeeCreated}' was not found.");

            var publishedVersion = await uow.WorkflowVersions.GetLatestPublishedByRailroadAndTriggerAsync(
                railroadCtrlNbr,
                employeeCreatedTriggerTypeCtrlNbr,
                ct);

            if (publishedVersion is null)
                throw new InvalidOperationException($"No published workflow found for trigger '{WorkflowTriggerTypeCodes.EmployeeCreated}' and railroad {railroadCtrlNbr.Value}.");

            var definition = DeserializeDefinition(publishedVersion.DefinitionJson);

            if (!ShouldExecuteConditions(definition.TriggerConditionGroupOperator, definition.TriggerConditions, metadata, referenceData))
            {
                var skippedHistory = WorkflowExecutionHistory.Start(
                    publishedVersion.WorkflowTemplateCtrlNbr,
                    publishedVersion.CtrlNbr,
                    publishedVersion.VersionNumber,
                    railroadCtrlNbr,
                    employeeCreatedTriggerTypeCtrlNbr,
                    employeeCtrlNbr,
                    domainEvent.CorrelationId);

                skippedHistory.Complete(
                    WorkflowExecutionStatus.Skipped,
                    JsonSerializer.Serialize(new
                    {
                        Reason = "Trigger conditions did not match.",
                        TriggerConditionGroupOperator = definition.TriggerConditionGroupOperator,
                        TriggerConditions = definition.TriggerConditions
                    }, JsonOptions));

                uow.WorkflowExecutionHistories.Add(skippedHistory);
                await uow.CommitAsync(ct);
                return;
            }

            var executionHistory = WorkflowExecutionHistory.Start(
                publishedVersion.WorkflowTemplateCtrlNbr,
                publishedVersion.CtrlNbr,
                publishedVersion.VersionNumber,
                railroadCtrlNbr,
                employeeCreatedTriggerTypeCtrlNbr,
                employeeCtrlNbr,
                domainEvent.CorrelationId);

            uow.WorkflowExecutionHistories.Add(executionHistory);
            var runtimeContext = BuildEffectRuntimeContext(primaryEmail, payload, railroadCtrlNbr);
            var triggerReferenceData = BuildTriggerReferenceData(referenceData);

            try
            {
                var triggerResult = await workflowTriggerExecutionTemplate.ExecuteAsync(
                    new WorkflowTriggerExecutionContext(
                        uow,
                        definition,
                        publishedVersion,
                        employeeCreatedTriggerTypeCtrlNbr,
                        railroadCtrlNbr,
                        employeeCtrlNbr,
                        triggerReferenceData,
                        new WorkflowRuntimeTriggerMetadata(metadata.ValuesByFieldCode),
                        runtimeContext,
                        domainEvent.CorrelationId,
                        ct));

                executionHistory.Complete(
                    triggerResult.Status,
                    JsonSerializer.Serialize(new { Steps = triggerResult.StepOutcomes }, JsonOptions));

                postCommitWorkItems = triggerResult.PostCommitWorkItems;
                await uow.CommitAsync(ct);
            }
            catch (WorkflowTriggerExecutionException ex)
            {
                executionHistory.Complete(
                    WorkflowExecutionStatus.Failed,
                    JsonSerializer.Serialize(new { Steps = ex.StepOutcomes }, JsonOptions));

                await uow.CommitAsync(ct);
                throw ex.InnerException ?? ex;
            }
            catch
            {
                executionHistory.Complete(
                    WorkflowExecutionStatus.Failed,
                    JsonSerializer.Serialize(new { Steps = Array.Empty<WorkflowExecutionStepOutcomeRecord>() }, JsonOptions));

                await uow.CommitAsync(ct);
                throw;
            }
        }

        await workflowPostCommitDispatcher.DispatchAsync(postCommitWorkItems, ct);
    }

    public async Task<bool> ExecuteSeniorityStatusChangedAsync(
        ControlNumber employeeCtrlNbr,
        ControlNumber newSeniorityStateCtrlNbr,
        ControlNumber rosterCtrlNbr,
        CancellationToken ct = default)
    {
        IReadOnlyList<WorkflowEffectPostCommitWorkItem> postCommitWorkItems;

        {
            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

            var roster = await uow.Rosters.GetByCtrlNbrAsync(rosterCtrlNbr, ct);
            if (roster is null)
            {
                logger.LogWarning(
                    "WorkflowRuntimeService: Seniority-status trigger skipped because roster {Roster} was not found.",
                    rosterCtrlNbr.Value);
                return false;
            }

            var railroadCtrlNbr = await railroadResolver.ResolveFromWorkAreaAsync(uow, roster.WorkAreaGroupCtrlNbr, ct);
            if (railroadCtrlNbr is null)
            {
                logger.LogWarning(
                    "WorkflowRuntimeService: Seniority-status trigger skipped because railroad could not be resolved from work area {WorkArea}.",
                    roster.WorkAreaGroupCtrlNbr.Value);
                return false;
            }

            var referenceData = await LoadReferenceDataAsync(uow, ct);
            if (!referenceData.TriggerTypeCtrlNbrByCode.TryGetValue(WorkflowTriggerTypeCodes.SeniorityStatusChanged, out var seniorityStatusChangedTriggerTypeCtrlNbr))
                throw new InvalidOperationException($"Workflow trigger type '{WorkflowTriggerTypeCodes.SeniorityStatusChanged}' was not found.");

            var publishedVersion = await uow.WorkflowVersions.GetLatestPublishedByRailroadAndTriggerAsync(
                railroadCtrlNbr,
                seniorityStatusChangedTriggerTypeCtrlNbr,
                ct);

            if (publishedVersion is null)
            {
                logger.LogInformation(
                    "WorkflowRuntimeService: No published workflow found for trigger '{TriggerType}' and railroad {Railroad}; skipping.",
                    WorkflowTriggerTypeCodes.SeniorityStatusChanged,
                    railroadCtrlNbr.Value);
                return false;
            }

            var seniorityState = await uow.SeniorityStates.GetByCtrlNbrAsync(newSeniorityStateCtrlNbr, ct)
                ?? throw new InvalidOperationException(
                    $"Seniority status {newSeniorityStateCtrlNbr.Value} was not found for workflow trigger.");

            var metadata = await BuildSeniorityWorkflowMetadataAsync(
                uow,
                rosterCtrlNbr,
                seniorityState,
                ct);
            var definition = DeserializeDefinition(publishedVersion.DefinitionJson);

            if (!ShouldExecuteConditions(definition.TriggerConditionGroupOperator, definition.TriggerConditions, metadata, referenceData))
            {
                var skippedHistory = WorkflowExecutionHistory.Start(
                    publishedVersion.WorkflowTemplateCtrlNbr,
                    publishedVersion.CtrlNbr,
                    publishedVersion.VersionNumber,
                    railroadCtrlNbr,
                    seniorityStatusChangedTriggerTypeCtrlNbr,
                    employeeCtrlNbr,
                    null);

                skippedHistory.Complete(
                    WorkflowExecutionStatus.Skipped,
                    JsonSerializer.Serialize(new
                    {
                        Reason = "Trigger conditions did not match.",
                        TriggerConditionGroupOperator = definition.TriggerConditionGroupOperator,
                        TriggerConditions = definition.TriggerConditions
                    }, JsonOptions));

                uow.WorkflowExecutionHistories.Add(skippedHistory);
                await uow.CommitAsync(ct);
                return false;
            }

            var executionHistory = WorkflowExecutionHistory.Start(
                publishedVersion.WorkflowTemplateCtrlNbr,
                publishedVersion.CtrlNbr,
                publishedVersion.VersionNumber,
                railroadCtrlNbr,
                seniorityStatusChangedTriggerTypeCtrlNbr,
                employeeCtrlNbr,
                null);

            uow.WorkflowExecutionHistories.Add(executionHistory);
            var triggerReferenceData = BuildTriggerReferenceData(referenceData);
            var runtimeContext = BuildSeniorityEffectRuntimeContext(
                employeeCtrlNbr,
                rosterCtrlNbr,
                newSeniorityStateCtrlNbr,
                railroadCtrlNbr);

            try
            {
                var triggerResult = await workflowTriggerExecutionTemplate.ExecuteAsync(
                    new WorkflowTriggerExecutionContext(
                        uow,
                        definition,
                        publishedVersion,
                        seniorityStatusChangedTriggerTypeCtrlNbr,
                        railroadCtrlNbr,
                        employeeCtrlNbr,
                        triggerReferenceData,
                        new WorkflowRuntimeTriggerMetadata(metadata.ValuesByFieldCode),
                        runtimeContext,
                        null,
                        ct));

                executionHistory.Complete(
                    triggerResult.Status,
                    JsonSerializer.Serialize(new { Steps = triggerResult.StepOutcomes }, JsonOptions));

                postCommitWorkItems = triggerResult.PostCommitWorkItems;
                await uow.CommitAsync(ct);
            }
            catch (WorkflowTriggerExecutionException ex)
            {
                executionHistory.Complete(
                    WorkflowExecutionStatus.Failed,
                    JsonSerializer.Serialize(new { Steps = ex.StepOutcomes }, JsonOptions));

                await uow.CommitAsync(ct);
                throw ex.InnerException ?? ex;
            }
            catch
            {
                executionHistory.Complete(
                    WorkflowExecutionStatus.Failed,
                    JsonSerializer.Serialize(new { Steps = Array.Empty<WorkflowExecutionStepOutcomeRecord>() }, JsonOptions));

                await uow.CommitAsync(ct);
                throw;
            }
        }

        await workflowPostCommitDispatcher.DispatchAsync(postCommitWorkItems, ct);
        return true;
    }

    public async Task<bool> ExecuteNotificationAcceptedAsync(
        ControlNumber railroadCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string notificationType,
        string? notificationBoardType,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        IReadOnlyList<WorkflowEffectPostCommitWorkItem> postCommitWorkItems;

        {
            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

            var referenceData = await LoadReferenceDataAsync(uow, ct);
            if (!referenceData.TriggerTypeCtrlNbrByCode.TryGetValue(
                    WorkflowTriggerTypeCodes.NotificationAccepted,
                    out var triggerTypeCtrlNbr))
            {
                throw new InvalidOperationException(
                    $"Workflow trigger type '{WorkflowTriggerTypeCodes.NotificationAccepted}' was not found.");
            }

            var publishedVersion = await uow.WorkflowVersions.GetLatestPublishedByRailroadAndTriggerAsync(
                railroadCtrlNbr,
                triggerTypeCtrlNbr,
                ct);

            if (publishedVersion is null)
            {
                logger.LogInformation(
                    "WorkflowRuntimeService: No published workflow found for trigger '{TriggerType}' and railroad {Railroad}; skipping.",
                    WorkflowTriggerTypeCodes.NotificationAccepted,
                    railroadCtrlNbr.Value);
                return false;
            }

            var definition = DeserializeDefinition(publishedVersion.DefinitionJson);
            var metadata = new WorkflowMetadataContext();
            metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.NotificationType] = notificationType;
            if (!string.IsNullOrWhiteSpace(notificationBoardType))
                metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.BoardType] = notificationBoardType;

            if (!ShouldExecuteConditions(definition.TriggerConditionGroupOperator, definition.TriggerConditions, metadata, referenceData))
            {
                var skippedHistory = WorkflowExecutionHistory.Start(
                    publishedVersion.WorkflowTemplateCtrlNbr,
                    publishedVersion.CtrlNbr,
                    publishedVersion.VersionNumber,
                    railroadCtrlNbr,
                    triggerTypeCtrlNbr,
                    employeeCtrlNbr,
                    correlationId);

                skippedHistory.Complete(
                    WorkflowExecutionStatus.Skipped,
                    JsonSerializer.Serialize(new
                    {
                        Reason = "Trigger conditions did not match.",
                        TriggerConditionGroupOperator = definition.TriggerConditionGroupOperator,
                        TriggerConditions = definition.TriggerConditions
                    }, JsonOptions));

                uow.WorkflowExecutionHistories.Add(skippedHistory);
                await uow.CommitAsync(ct);
                return true;
            }

            var executionHistory = WorkflowExecutionHistory.Start(
                publishedVersion.WorkflowTemplateCtrlNbr,
                publishedVersion.CtrlNbr,
                publishedVersion.VersionNumber,
                railroadCtrlNbr,
                triggerTypeCtrlNbr,
                employeeCtrlNbr,
                correlationId);

            uow.WorkflowExecutionHistories.Add(executionHistory);
            var triggerReferenceData = BuildTriggerReferenceData(referenceData);
            var runtimeContext = BuildNotificationAcceptedRuntimeContext(railroadCtrlNbr, employeeCtrlNbr);

            try
            {
                var triggerResult = await workflowTriggerExecutionTemplate.ExecuteAsync(
                    new WorkflowTriggerExecutionContext(
                        uow,
                        definition,
                        publishedVersion,
                        triggerTypeCtrlNbr,
                        railroadCtrlNbr,
                        employeeCtrlNbr,
                        triggerReferenceData,
                        new WorkflowRuntimeTriggerMetadata(metadata.ValuesByFieldCode),
                        runtimeContext,
                        correlationId,
                        ct));

                executionHistory.Complete(
                    triggerResult.Status,
                    JsonSerializer.Serialize(new { Steps = triggerResult.StepOutcomes }, JsonOptions));

                postCommitWorkItems = triggerResult.PostCommitWorkItems;
                await uow.CommitAsync(ct);
            }
            catch (WorkflowTriggerExecutionException ex)
            {
                executionHistory.Complete(
                    WorkflowExecutionStatus.Failed,
                    JsonSerializer.Serialize(new { Steps = ex.StepOutcomes }, JsonOptions));

                await uow.CommitAsync(ct);
                throw ex.InnerException ?? ex;
            }
            catch
            {
                executionHistory.Complete(
                    WorkflowExecutionStatus.Failed,
                    JsonSerializer.Serialize(new { Steps = Array.Empty<WorkflowExecutionStepOutcomeRecord>() }, JsonOptions));

                await uow.CommitAsync(ct);
                throw;
            }
        }

        await workflowPostCommitDispatcher.DispatchAsync(postCommitWorkItems, ct);
        return true;
    }

    public async Task<bool> ExecuteVacancyPlaceOnDutyRequestedAsync(
        ControlNumber railroadCtrlNbr,
        WorkflowPlaceOnDutyRuntimePayload payload,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        IReadOnlyList<WorkflowEffectPostCommitWorkItem> postCommitWorkItems;

        {
            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

            var referenceData = await LoadReferenceDataAsync(uow, ct);
            if (!referenceData.TriggerTypeCtrlNbrByCode.TryGetValue(
                    WorkflowTriggerTypeCodes.VacancyPlaceOnDutyRequested,
                    out var triggerTypeCtrlNbr))
            {
                throw new InvalidOperationException(
                    $"Workflow trigger type '{WorkflowTriggerTypeCodes.VacancyPlaceOnDutyRequested}' was not found.");
            }

            var publishedVersion = await uow.WorkflowVersions.GetLatestPublishedByRailroadAndTriggerAsync(
                railroadCtrlNbr,
                triggerTypeCtrlNbr,
                ct);

            if (publishedVersion is null)
            {
                logger.LogInformation(
                    "WorkflowRuntimeService: No published workflow found for trigger '{TriggerType}' and railroad {Railroad}; skipping.",
                    WorkflowTriggerTypeCodes.VacancyPlaceOnDutyRequested,
                    railroadCtrlNbr.Value);
                return false;
            }

            var definition = DeserializeDefinition(publishedVersion.DefinitionJson);
            var metadata = new WorkflowMetadataContext();

            if (!ShouldExecuteConditions(definition.TriggerConditionGroupOperator, definition.TriggerConditions, metadata, referenceData))
            {
                var skippedHistory = WorkflowExecutionHistory.Start(
                    publishedVersion.WorkflowTemplateCtrlNbr,
                    publishedVersion.CtrlNbr,
                    publishedVersion.VersionNumber,
                    railroadCtrlNbr,
                    triggerTypeCtrlNbr,
                    payload.EmployeeCtrlNbr,
                    correlationId);

                skippedHistory.Complete(
                    WorkflowExecutionStatus.Skipped,
                    JsonSerializer.Serialize(new
                    {
                        Reason = "Trigger conditions did not match.",
                        TriggerConditionGroupOperator = definition.TriggerConditionGroupOperator,
                        TriggerConditions = definition.TriggerConditions
                    }, JsonOptions));

                uow.WorkflowExecutionHistories.Add(skippedHistory);
                await uow.CommitAsync(ct);
                return false;
            }

            var executionHistory = WorkflowExecutionHistory.Start(
                publishedVersion.WorkflowTemplateCtrlNbr,
                publishedVersion.CtrlNbr,
                publishedVersion.VersionNumber,
                railroadCtrlNbr,
                triggerTypeCtrlNbr,
                payload.EmployeeCtrlNbr,
                correlationId);

            uow.WorkflowExecutionHistories.Add(executionHistory);
            var triggerReferenceData = BuildTriggerReferenceData(referenceData);
            var runtimeContext = BuildVacancyPlaceOnDutyRuntimeContext(railroadCtrlNbr, payload);

            try
            {
                var triggerResult = await workflowTriggerExecutionTemplate.ExecuteAsync(
                    new WorkflowTriggerExecutionContext(
                        uow,
                        definition,
                        publishedVersion,
                        triggerTypeCtrlNbr,
                        railroadCtrlNbr,
                        payload.EmployeeCtrlNbr,
                        triggerReferenceData,
                        new WorkflowRuntimeTriggerMetadata(metadata.ValuesByFieldCode),
                        runtimeContext,
                        correlationId,
                        ct));

                executionHistory.Complete(
                    triggerResult.Status,
                    JsonSerializer.Serialize(new { Steps = triggerResult.StepOutcomes }, JsonOptions));

                postCommitWorkItems = triggerResult.PostCommitWorkItems;
                await uow.CommitAsync(ct);
            }
            catch (WorkflowTriggerExecutionException ex)
            {
                executionHistory.Complete(
                    WorkflowExecutionStatus.Failed,
                    JsonSerializer.Serialize(new { Steps = ex.StepOutcomes }, JsonOptions));

                await uow.CommitAsync(ct);
                throw ex.InnerException ?? ex;
            }
            catch
            {
                executionHistory.Complete(
                    WorkflowExecutionStatus.Failed,
                    JsonSerializer.Serialize(new { Steps = Array.Empty<WorkflowExecutionStepOutcomeRecord>() }, JsonOptions));

                await uow.CommitAsync(ct);
                throw;
            }
        }

        await workflowPostCommitDispatcher.DispatchAsync(postCommitWorkItems, ct);
        return true;
    }

    private static ControlNumber ResolveRailroadCtrlNbr(long? payloadRailroadCtrlNbr, WorkflowMetadataContext metadata)
    {
        if (payloadRailroadCtrlNbr is > 0)
            return ControlNumber.Create(payloadRailroadCtrlNbr.Value);

        if (metadata.CraftRailroadCtrlNbr is not null)
            return metadata.CraftRailroadCtrlNbr;

        throw new InvalidOperationException("Unable to resolve railroad scope for Employee Created workflow execution.");
    }

    private static WorkflowDefinition DeserializeDefinition(string definitionJson)
    {
        var definition = JsonSerializer.Deserialize<WorkflowDefinition>(definitionJson, JsonOptions);
        if (definition is null)
            throw new InvalidOperationException("Workflow definition could not be deserialized.");

        return definition;
    }

    private static bool ShouldExecuteConditions(
        string conditionGroupOperator,
        IReadOnlyList<WorkflowConditionDefinition> conditions,
        WorkflowMetadataContext metadata,
        WorkflowReferenceData referenceData)
    {
        if (conditions.Count == 0)
            return true;

        var useAll = !string.Equals(conditionGroupOperator, "ANY", StringComparison.OrdinalIgnoreCase);

        return useAll
            ? conditions.All(c => EvaluateCondition(c, metadata, referenceData))
            : conditions.Any(c => EvaluateCondition(c, metadata, referenceData));
    }

    private static bool EvaluateCondition(WorkflowConditionDefinition condition, WorkflowMetadataContext metadata, WorkflowReferenceData referenceData)
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


    private static EmployeeCreatedTriggerPayload ParsePayload(DomainEvent domainEvent)
    {
        if (string.IsNullOrWhiteSpace(domainEvent.PayloadJson))
            throw new InvalidOperationException("Employee Created domain event payload is missing.");

        var payload = JsonSerializer.Deserialize<EmployeeCreatedTriggerPayload>(domainEvent.PayloadJson, JsonOptions);
        if (payload is null)
            throw new InvalidOperationException("Employee Created domain event payload is invalid.");

        return payload;
    }

    private static WorkflowRuntimeTriggerReferenceData BuildTriggerReferenceData(WorkflowReferenceData referenceData)
    {
        return new WorkflowRuntimeTriggerReferenceData(
            referenceData.EffectTypeCodeByCtrlNbr,
            referenceData.OperatorCodeByCtrlNbr,
            referenceData.MetadataFieldCodeByCtrlNbr);
    }

    private static WorkflowEffectRuntimeContext BuildEffectRuntimeContext(
        string? primaryEmail,
        EmployeeCreatedTriggerPayload payload,
        ControlNumber triggerRailroadCtrlNbr)
    {
        return new WorkflowEffectRuntimeContext(
            primaryEmail,
            payload.ClientCtrlNbr,
            payload.Email,
            payload.InvitedByUserId,
            payload.InvitedByUserName,
            triggerRailroadCtrlNbr,
            EmployeeCtrlNbr: null,
            RosterCtrlNbr: null,
            SeniorityStateCtrlNbr: null);
    }

    private static WorkflowEffectRuntimeContext BuildSeniorityEffectRuntimeContext(
        ControlNumber employeeCtrlNbr,
        ControlNumber rosterCtrlNbr,
        ControlNumber newSeniorityStateCtrlNbr,
        ControlNumber triggerRailroadCtrlNbr)
    {
        return new WorkflowEffectRuntimeContext(
            PrimaryEmail: null,
            ClientCtrlNbr: 0,
            TriggerEmail: string.Empty,
            InvitedByUserId: string.Empty,
            InvitedByUserName: string.Empty,
            TriggerRailroadCtrlNbr: triggerRailroadCtrlNbr,
            EmployeeCtrlNbr: employeeCtrlNbr,
            RosterCtrlNbr: rosterCtrlNbr,
            SeniorityStateCtrlNbr: newSeniorityStateCtrlNbr);
    }

    private static WorkflowEffectRuntimeContext BuildVacancyPlaceOnDutyRuntimeContext(
        ControlNumber railroadCtrlNbr,
        WorkflowPlaceOnDutyRuntimePayload payload)
    {
        return new WorkflowEffectRuntimeContext(
            PrimaryEmail: null,
            ClientCtrlNbr: 0,
            TriggerEmail: string.Empty,
            InvitedByUserId: string.Empty,
            InvitedByUserName: string.Empty,
            TriggerRailroadCtrlNbr: railroadCtrlNbr,
            EmployeeCtrlNbr: payload.EmployeeCtrlNbr,
            RosterCtrlNbr: null,
            SeniorityStateCtrlNbr: null,
            PlaceOnDutyPayload: payload);
    }

    private static WorkflowEffectRuntimeContext BuildNotificationAcceptedRuntimeContext(
        ControlNumber railroadCtrlNbr,
        ControlNumber employeeCtrlNbr)
    {
        return new WorkflowEffectRuntimeContext(
            PrimaryEmail: null,
            ClientCtrlNbr: 0,
            TriggerEmail: string.Empty,
            InvitedByUserId: string.Empty,
            InvitedByUserName: string.Empty,
            TriggerRailroadCtrlNbr: railroadCtrlNbr,
            EmployeeCtrlNbr: employeeCtrlNbr,
            RosterCtrlNbr: null,
            SeniorityStateCtrlNbr: null,
            PlaceOnDutyPayload: null);
    }

    private static async Task<WorkflowMetadataContext> BuildEmployeeMetadataAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct)
    {
        var metadata = new WorkflowMetadataContext();

        var seniorityRows = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        var selectedSeniority = seniorityRows
            .OrderByDescending(s => s.LastActiveRoster)
            .ThenByDescending(s => s.RosterDate)
            .FirstOrDefault();

        if (selectedSeniority is null)
            return metadata;

        var roster = await uow.Rosters.GetByCtrlNbrAsync(selectedSeniority.RosterCtrlNbr, ct);
        if (roster is not null)
        {
            var craft = await uow.Crafts.GetByCtrlNbrAsync(roster.CraftCtrlNbr, ct);
            if (craft is not null)
            {
                metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.CraftCtrlNbr] = craft.CtrlNbr.Value.ToString();
                metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.CraftName] = craft.CraftName;
                metadata.CraftRailroadCtrlNbr = craft.DynamicGroupCtrlNbr;

                if (craft.DepartmentCtrlNbr is not null)
                {
                    metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.DepartmentCtrlNbr] = craft.DepartmentCtrlNbr.Value.ToString();

                    var department = await uow.Departments.GetByCtrlNbrAsync(craft.DepartmentCtrlNbr.Value, ct);
                    if (department is not null)
                        metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.DepartmentName] = department.Name;
                }
            }
        }

        var seniorityState = await uow.SeniorityStates.GetByCtrlNbrAsync(selectedSeniority.SeniorityStateCtrlNbr, ct);
        if (seniorityState is not null)
        {
            metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.SeniorityStateCtrlNbr] = seniorityState.CtrlNbr.Value.ToString();
            metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.SeniorityStateName] = seniorityState.StateDescription;
        }

        return metadata;
    }

    private static WorkflowMetadataContext BuildSeniorityStatusMetadata(SeniorityState seniorityState)
    {
        var metadata = new WorkflowMetadataContext();
        metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.NewSeniorityState] = seniorityState.StateDescription;
        metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.SeniorityStateCtrlNbr] = seniorityState.CtrlNbr.Value.ToString();
        metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.SeniorityStateName] = seniorityState.StateDescription;
        return metadata;
    }

    private static async Task<WorkflowMetadataContext> BuildSeniorityWorkflowMetadataAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber rosterCtrlNbr,
        SeniorityState seniorityState,
        CancellationToken ct)
    {
        var metadata = BuildSeniorityStatusMetadata(seniorityState);

        var roster = await uow.Rosters.GetByCtrlNbrAsync(rosterCtrlNbr, ct);
        if (roster is null)
            return metadata;

        var craft = await uow.Crafts.GetByCtrlNbrAsync(roster.CraftCtrlNbr, ct);
        if (craft is null)
            return metadata;

        metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.CraftCtrlNbr] = craft.CtrlNbr.Value.ToString();
        metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.CraftName] = craft.CraftName;

        if (craft.DepartmentCtrlNbr is null)
            return metadata;

        metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.DepartmentCtrlNbr] = craft.DepartmentCtrlNbr.Value.ToString();

        var department = await uow.Departments.GetByCtrlNbrAsync(craft.DepartmentCtrlNbr.Value, ct);
        if (department is not null)
            metadata.ValuesByFieldCode[WorkflowMetadataFieldTypeCodes.DepartmentName] = department.Name;

        return metadata;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private sealed class WorkflowMetadataContext
    {
        public Dictionary<string, string> ValuesByFieldCode { get; } = new(StringComparer.OrdinalIgnoreCase);
        public ControlNumber? CraftRailroadCtrlNbr { get; set; }
    }

    private sealed class WorkflowReferenceData
    {
        public Dictionary<string, ControlNumber> TriggerTypeCtrlNbrByCode { get; } = new(StringComparer.Ordinal);
        public Dictionary<ControlNumber, string> EffectTypeCodeByCtrlNbr { get; } = [];
        public Dictionary<ControlNumber, string> OperatorCodeByCtrlNbr { get; } = [];
        public Dictionary<ControlNumber, string> MetadataFieldCodeByCtrlNbr { get; } = [];
    }

    private static async Task<WorkflowReferenceData> LoadReferenceDataAsync(IOrchestrationUnitOfWork uow, CancellationToken ct)
    {
        var referenceData = new WorkflowReferenceData();

        var triggerTypes = await uow.WorkflowTriggerTypes.GetAllActiveAsync(ct);
        foreach (var triggerType in triggerTypes)
            referenceData.TriggerTypeCtrlNbrByCode[triggerType.Code] = triggerType.CtrlNbr;

        var effectTypes = await uow.WorkflowEffectTypes.GetAllActiveAsync(ct);
        foreach (var effectType in effectTypes)
            referenceData.EffectTypeCodeByCtrlNbr[effectType.CtrlNbr] = effectType.Code;

        var operatorTypes = await uow.WorkflowOperatorTypes.GetAllActiveAsync(ct);
        foreach (var operatorType in operatorTypes)
            referenceData.OperatorCodeByCtrlNbr[operatorType.CtrlNbr] = operatorType.Code;

        var metadataFieldTypes = await uow.WorkflowMetadataFieldTypes.GetAllActiveAsync(ct);
        foreach (var metadataFieldType in metadataFieldTypes)
            referenceData.MetadataFieldCodeByCtrlNbr[metadataFieldType.CtrlNbr] = metadataFieldType.Code;

        return referenceData;
    }

    private sealed record EmployeeCreatedTriggerPayload(
        long AggregateCtrlNbr,
        long ClientCtrlNbr,
        long? RailroadCtrlNbr,
        string Email,
        string InvitedByUserId,
        string InvitedByUserName,
        string ParentName);
}

public static class TriggerTypes
{
    public const string EmployeeCreated = "Employee Created";
    public const string SeniorityStatusChanged = "Seniority Status Changed";
    public const string VacancyPlaceOnDutyRequested = "Vacancy Place On Duty Requested";
    public const string NotificationAccepted = "Notification Accepted";
}

public static class WorkflowEffectTypes
{
    public const string SendInvitation = "Send Invitation";
    public const string DoNothing = "Do Nothing";
    public const string AddToRosterBoard = "Add to Roster Board";
    public const string VacatePositionAndBulletinPosition = "Vacate Position & Bulletin Position";
    public const string PlaceOnDuty = "Place On Duty";
    public const string CreateSeniorityMove = "Create Seniority Move";
}

public static class WorkflowFailurePolicies
{
    public const string StopWorkflow = "StopWorkflow";
}

public static class WorkflowOptionKeys
{
    public const string EffectOption = "effectOption";
    public const string RoleCtrlNbr = "roleCtrlNbr";
    public const string ExpirationDays = "expirationDays";
    public const string RailroadCtrlNbr = "railroadCtrlNbr";
    public const string UsePrimaryEmail = "usePrimaryEmail";
    public const string BoardType = "boardType";
    public const string AutoMoveDelayHours = "autoMoveDelayHours";
}

public static class WorkflowMetadataKeys
{
    public const string NewSeniorityState = "newSeniorityState";
    public const string DepartmentCtrlNbr = "departmentCtrlNbr";
    public const string DepartmentName = "departmentName";
    public const string CraftCtrlNbr = "craftCtrlNbr";
    public const string CraftName = "craftName";
    public const string SeniorityStateCtrlNbr = "seniorityStateCtrlNbr";
    public const string SeniorityStateName = "seniorityStateName";
    public const string NotificationType = "notificationType";
    public const string BoardType = "boardType";
}

public static class WorkflowOperators
{
    public const string EqualsOperator = "Equals";
    public const string NotEqualsOperator = "NotEquals";
}

public static class WorkflowTriggerTypeCodes
{
    public const string EmployeeCreated = TriggerTypes.EmployeeCreated;
    public const string SeniorityStatusChanged = TriggerTypes.SeniorityStatusChanged;
    public const string VacancyPlaceOnDutyRequested = TriggerTypes.VacancyPlaceOnDutyRequested;
    public const string NotificationAccepted = TriggerTypes.NotificationAccepted;
}

public static class WorkflowEffectTypeCodes
{
    public const string SendInvitation = WorkflowEffectTypes.SendInvitation;
    public const string DoNothing = WorkflowEffectTypes.DoNothing;
    public const string AddToRosterBoard = WorkflowEffectTypes.AddToRosterBoard;
    public const string VacatePositionAndBulletinPosition = WorkflowEffectTypes.VacatePositionAndBulletinPosition;
    public const string PlaceOnDuty = WorkflowEffectTypes.PlaceOnDuty;
    public const string CreateSeniorityMove = WorkflowEffectTypes.CreateSeniorityMove;
}

public static class WorkflowOperatorTypeCodes
{
    public const string EqualsOperator = WorkflowOperators.EqualsOperator;
    public const string NotEquals = WorkflowOperators.NotEqualsOperator;
}

public static class WorkflowMetadataFieldTypeCodes
{
    public const string NewSeniorityState = WorkflowMetadataKeys.NewSeniorityState;
    public const string DepartmentCtrlNbr = WorkflowMetadataKeys.DepartmentCtrlNbr;
    public const string DepartmentName = WorkflowMetadataKeys.DepartmentName;
    public const string CraftCtrlNbr = WorkflowMetadataKeys.CraftCtrlNbr;
    public const string CraftName = WorkflowMetadataKeys.CraftName;
    public const string SeniorityStateCtrlNbr = WorkflowMetadataKeys.SeniorityStateCtrlNbr;
    public const string SeniorityStateName = WorkflowMetadataKeys.SeniorityStateName;
    public const string NotificationType = WorkflowMetadataKeys.NotificationType;
    public const string BoardType = WorkflowMetadataKeys.BoardType;
}
