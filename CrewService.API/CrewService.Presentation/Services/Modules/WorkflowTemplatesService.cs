using CrewService.Application.Workflows;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public sealed class WorkflowTemplatesService(IServiceProvider serviceProvider) : WorkflowTemplatesSrvc.WorkflowTemplatesSrvcBase
{
    public override async Task<GetWorkflowTemplatesResponse> GetByRailroad(GetWorkflowTemplatesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<WorkflowTemplateManagementService>();
        var workflows = await svc.GetByRailroadAsync(ControlNumber.Create(request.RailroadCtrlNbr), context.CancellationToken);

        var response = new GetWorkflowTemplatesResponse();
        response.Workflows.AddRange(workflows.Select(MapSummary));
        return response;
    }

    public override async Task<WorkflowTemplateDetailResponse> GetByCtrlNbr(GetWorkflowTemplateRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<WorkflowTemplateManagementService>();

        try
        {
            var detail = await svc.GetDetailAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return MapDetail(detail);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetWorkflowReferenceCatalogResponse> GetReferenceCatalog(
        GetWorkflowReferenceCatalogRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<WorkflowTemplateManagementService>();

        var catalog = await svc.GetReferenceCatalogAsync(
            ControlNumber.Create(request.RailroadCtrlNbr),
            context.CancellationToken);

        var response = new GetWorkflowReferenceCatalogResponse();
        response.TriggerTypes.AddRange(catalog.TriggerTypes.Select(MapReferenceItemToTriggerType));
        response.EffectTypes.AddRange(catalog.EffectTypes.Select(MapReferenceItemToEffectType));
        response.OperatorTypes.AddRange(catalog.OperatorTypes.Select(MapReferenceItemToOperatorType));
        response.MetadataFieldTypes.AddRange(catalog.MetadataFieldTypes.Select(MapReferenceItemToMetadataFieldType));
        response.TriggerMetadataFieldMaps.AddRange(catalog.TriggerMetadataFieldMaps.Select(m => new WorkflowTriggerMetadataFieldMap
        {
            TriggerTypeCtrlNbr = m.TriggerTypeCtrlNbr.Value,
            MetadataFieldTypeCtrlNbr = m.MetadataFieldTypeCtrlNbr.Value
        }));

        return response;
    }

    public override async Task<GetWorkflowTemplateExecutionHistoryResponse> GetExecutionHistory(
        GetWorkflowTemplateExecutionHistoryRequest request,
        ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<WorkflowTemplateManagementService>();

        try
        {
            var entries = await svc.GetExecutionHistoryAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.Take <= 0 ? 100 : request.Take,
                context.CancellationToken);

            var response = new GetWorkflowTemplateExecutionHistoryResponse();
            response.Entries.AddRange(entries.Select(MapExecutionHistory));
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<WorkflowTemplateDetailResponse> SetEnabled(SetWorkflowTemplateEnabledRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<WorkflowTemplateManagementService>();

        try
        {
            var updated = await svc.SetEnabledAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.IsEnabled,
                context.CancellationToken);

            return MapDetail(updated);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<WorkflowTemplateDetailResponse> Create(CreateWorkflowTemplateRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<WorkflowTemplateManagementService>();

        try
        {
            var created = await svc.CreateTemplateAsync(
                railroadCtrlNbr: ControlNumber.Create(request.RailroadCtrlNbr),
                name: request.Name,
                canStartFromTrigger: request.CanStartFromTrigger,
                triggerTypeCtrlNbr: request.TriggerTypeCtrlNbr > 0 ? ControlNumber.Create(request.TriggerTypeCtrlNbr) : null,
                isEnabled: request.IsEnabled,
                ct: context.CancellationToken);

            return MapDetail(created);
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<WorkflowTemplateDetailResponse> SaveDraft(SaveWorkflowTemplateDraftRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<WorkflowTemplateManagementService>();

        try
        {
            var saved = await svc.SaveDraftAsync(
                ControlNumber.Create(request.CtrlNbr),
                MapUpsertRequest(request.Template),
                context.CancellationToken);

            return MapDetail(saved);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<WorkflowTemplateDetailResponse> Publish(PublishWorkflowTemplateRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<WorkflowTemplateManagementService>();

        try
        {
            var published = await svc.PublishAsync(
                ControlNumber.Create(request.CtrlNbr),
                MapUpsertRequest(request.Template),
                context.CancellationToken);

            return MapDetail(published);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<WorkflowTemplateDetailResponse> RestoreAsDraft(RestoreWorkflowTemplateVersionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<WorkflowTemplateManagementService>();

        try
        {
            var restored = await svc.RestoreVersionAsDraftAsync(
                ControlNumber.Create(request.CtrlNbr),
                request.VersionNumber,
                request.Notes,
                context.CancellationToken);

            return MapDetail(restored);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<DeleteResponse> Delete(DeleteWorkflowTemplateRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<WorkflowTemplateManagementService>();

        try
        {
            await svc.DeleteTemplateAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true, Messages = { "Workflow template deleted." } };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static WorkflowTemplateSummaryResponse MapSummary(WorkflowTemplateSummary source)
    {
        return new WorkflowTemplateSummaryResponse
        {
            CtrlNbr = source.CtrlNbr.Value,
            Name = source.Name,
            CanStartFromTrigger = source.CanStartFromTrigger,
            TriggerTypeCtrlNbr = source.TriggerTypeCtrlNbr?.Value ?? 0,
            IsEnabled = source.IsEnabled,
            Status = source.Status,
            CurrentVersionNumber = source.CurrentVersionNumber,
            StepCount = source.StepCount
        };
    }

    private static WorkflowExecutionHistoryResponse MapExecutionHistory(WorkflowExecutionHistoryDto source)
    {
        return new WorkflowExecutionHistoryResponse
        {
            CtrlNbr = source.CtrlNbr.Value,
            WorkflowTemplateCtrlNbr = source.WorkflowTemplateCtrlNbr.Value,
            WorkflowVersionCtrlNbr = source.WorkflowVersionCtrlNbr.Value,
            WorkflowVersionNumber = source.WorkflowVersionNumber,
            RailroadCtrlNbr = source.RailroadCtrlNbr.Value,
            TriggerTypeCtrlNbr = source.TriggerTypeCtrlNbr?.Value ?? 0,
            AggregateCtrlNbr = source.AggregateCtrlNbr?.Value ?? 0,
            AggregateDisplay = source.AggregateDisplay,
            CorrelationId = source.CorrelationId ?? string.Empty,
            StartedAtUtc = source.StartedAtUtc.ToString("O"),
            CompletedAtUtc = source.CompletedAtUtc?.ToString("O") ?? string.Empty,
            Status = source.Status,
            DetailsJson = source.DetailsJson ?? string.Empty,
            DetailsDisplayJson = source.DetailsDisplayJson ?? string.Empty
        };
    }

    private static WorkflowTemplateDetailResponse MapDetail(WorkflowTemplateDetail source)
    {
        var response = new WorkflowTemplateDetailResponse
        {
            CtrlNbr = source.CtrlNbr.Value,
            RailroadCtrlNbr = source.RailroadCtrlNbr.Value,
            Name = source.Name,
            CanStartFromTrigger = source.CanStartFromTrigger,
            TriggerTypeCtrlNbr = source.TriggerTypeCtrlNbr?.Value ?? 0,
            IsEnabled = source.IsEnabled,
            Status = source.Status,
            CurrentVersionNumber = source.CurrentVersionNumber
        };

        response.TriggerConditionGroupOperator = source.TriggerConditionGroupOperator;
        response.TriggerConditions.AddRange(source.TriggerConditions.Select(c => new WorkflowConditionPayload
        {
            CtrlNbr = c.CtrlNbr.Value,
            FieldTypeCtrlNbr = c.FieldTypeCtrlNbr.Value,
            OperatorTypeCtrlNbr = c.OperatorTypeCtrlNbr.Value,
            Value = c.Value
        }));

        response.Steps.AddRange(source.Steps.Select(MapStep));
        response.Versions.AddRange(source.Versions.Select(MapVersion));
        return response;
    }

    private static WorkflowTemplateVersionResponse MapVersion(WorkflowTemplateVersionDto source)
    {
        return new WorkflowTemplateVersionResponse
        {
            VersionNumber = source.VersionNumber,
            Status = source.Status,
            SavedAtUtc = source.SavedAtUtc.ToString("O"),
            Notes = source.Notes,
            StepCount = source.StepCount,
            DefinitionJson = source.DefinitionJson
        };
    }

    private static WorkflowStepResponse MapStep(WorkflowStepDto source)
    {
        var response = new WorkflowStepResponse
        {
            CtrlNbr = source.CtrlNbr.Value,
            Order = source.Order,
            Name = source.Name,
            IsEnabled = source.IsEnabled,
            FailurePolicy = source.FailurePolicy,
            ConditionGroupOperator = source.ConditionGroupOperator
        };

        response.Conditions.AddRange(source.Conditions.Select(c => new WorkflowConditionPayload
        {
            CtrlNbr = c.CtrlNbr.Value,
            FieldTypeCtrlNbr = c.FieldTypeCtrlNbr.Value,
            OperatorTypeCtrlNbr = c.OperatorTypeCtrlNbr.Value,
            Value = c.Value
        }));

        response.Effects.AddRange(source.Effects.Select(MapEffect));

        return response;
    }

    private static WorkflowEffectPayload MapEffect(WorkflowEffectDto source)
    {
        var response = new WorkflowEffectPayload
        {
            CtrlNbr = source.CtrlNbr.Value,
            Order = source.Order,
            IsEnabled = source.IsEnabled,
            EffectTypeCtrlNbr = source.EffectTypeCtrlNbr.Value,
            EffectOption = source.EffectOption
        };

        response.Options.AddRange(source.Options.Select(o => new WorkflowEffectOptionPayload
        {
            Key = o.Key,
            Value = o.Value
        }));

        return response;
    }

    private static WorkflowTemplateUpsertRequest MapUpsertRequest(WorkflowTemplateUpsertPayload source)
    {
        return new WorkflowTemplateUpsertRequest(
            source.Name,
            source.CanStartFromTrigger,
            source.TriggerTypeCtrlNbr > 0 ? ControlNumber.Create(source.TriggerTypeCtrlNbr) : null,
            source.IsEnabled,
            source.VersionNotes,
            source.TriggerConditionGroupOperator,
            source.TriggerConditions.Select(MapCondition).ToList(),
            source.Steps.Select(MapStep).ToList());
    }

    private static WorkflowStepUpsertRequest MapStep(WorkflowStepUpsertPayload source)
    {
        return new WorkflowStepUpsertRequest(
            source.CtrlNbr > 0 ? ControlNumber.Create(source.CtrlNbr) : ControlNumber.Create(),
            source.Order,
            source.Name,
            source.IsEnabled,
            source.FailurePolicy,
            source.ConditionGroupOperator,
            source.Conditions.Select(MapCondition).ToList(),
            source.Effects.Select(MapEffect).ToList());
    }

    private static WorkflowConditionUpsertRequest MapCondition(WorkflowConditionPayload source)
    {
        return new WorkflowConditionUpsertRequest(
            source.CtrlNbr > 0 ? ControlNumber.Create(source.CtrlNbr) : ControlNumber.Create(),
            source.FieldTypeCtrlNbr > 0 ? ControlNumber.Create(source.FieldTypeCtrlNbr) : throw new InvalidOperationException("condition.fieldTypeCtrlNbr must be a valid control number."),
            source.OperatorTypeCtrlNbr > 0 ? ControlNumber.Create(source.OperatorTypeCtrlNbr) : throw new InvalidOperationException("condition.operatorTypeCtrlNbr must be a valid control number."),
            source.Value);
    }

    private static WorkflowEffectUpsertRequest MapEffect(WorkflowEffectPayload source)
    {
        return new WorkflowEffectUpsertRequest(
            source.CtrlNbr > 0 ? ControlNumber.Create(source.CtrlNbr) : ControlNumber.Create(),
            source.Order,
            source.IsEnabled,
            source.EffectTypeCtrlNbr > 0 ? ControlNumber.Create(source.EffectTypeCtrlNbr) : throw new InvalidOperationException("effect.effectTypeCtrlNbr must be a valid control number."),
            source.EffectOption,
            source.Options.Select(o => new WorkflowEffectOptionDto(o.Key, o.Value)).ToList());
    }

    private static WorkflowTriggerTypeReference MapReferenceItemToTriggerType(WorkflowReferenceItemDto source)
    {
        return new WorkflowTriggerTypeReference
        {
            CtrlNbr = source.CtrlNbr.Value,
            Code = source.Code,
            Name = source.Name
        };
    }

    private static WorkflowEffectTypeReference MapReferenceItemToEffectType(WorkflowReferenceItemDto source)
    {
        return new WorkflowEffectTypeReference
        {
            CtrlNbr = source.CtrlNbr.Value,
            Code = source.Code,
            Name = source.Name
        };
    }

    private static WorkflowOperatorTypeReference MapReferenceItemToOperatorType(WorkflowReferenceItemDto source)
    {
        return new WorkflowOperatorTypeReference
        {
            CtrlNbr = source.CtrlNbr.Value,
            Code = source.Code,
            Name = source.Name
        };
    }

    private static WorkflowMetadataFieldTypeReference MapReferenceItemToMetadataFieldType(WorkflowReferenceItemDto source)
    {
        return new WorkflowMetadataFieldTypeReference
        {
            CtrlNbr = source.CtrlNbr.Value,
            Code = source.Code,
            Name = source.Name
        };
    }
}
