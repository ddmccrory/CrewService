using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class WorkflowTemplatesClient(
    GrpcChannelProvider channelProvider,
    CircuitTokenProvider tokenProvider,
    AppContextService appContext,
    ILogger<WorkflowTemplatesClient> logger)
    : BaseGrpcClient<WorkflowTemplatesSrvc.WorkflowTemplatesSrvcClient>(
        channelProvider,
        tokenProvider,
        appContext,
        callInvoker => new WorkflowTemplatesSrvc.WorkflowTemplatesSrvcClient(callInvoker),
        logger)
{
    public async Task<GetWorkflowTemplatesResponse> GetByRailroadAsync(long railroadCtrlNbr)
    {
        try
        {
            return await _client.GetByRailroadAsync(new GetWorkflowTemplatesRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<WorkflowTemplateDetailResponse> GetByCtrlNbrAsync(long ctrlNbr)
    {
        try
        {
            return await _client.GetByCtrlNbrAsync(new GetWorkflowTemplateRequest
            {
                CtrlNbr = ctrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<WorkflowTemplateDetailResponse> CreateAsync(
        long railroadCtrlNbr,
        string name,
        bool canStartFromTrigger,
        long triggerTypeCtrlNbr,
        bool isEnabled)
    {
        try
        {
            return await _client.CreateAsync(new CreateWorkflowTemplateRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr,
                Name = name,
                CanStartFromTrigger = canStartFromTrigger,
                TriggerTypeCtrlNbr = triggerTypeCtrlNbr,
                IsEnabled = isEnabled
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<WorkflowTemplateDetailResponse> SaveDraftAsync(long ctrlNbr, WorkflowTemplateUpsertPayload template)
    {
        try
        {
            return await _client.SaveDraftAsync(new SaveWorkflowTemplateDraftRequest
            {
                CtrlNbr = ctrlNbr,
                Template = template
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<WorkflowTemplateDetailResponse> PublishAsync(long ctrlNbr, WorkflowTemplateUpsertPayload template)
    {
        try
        {
            return await _client.PublishAsync(new PublishWorkflowTemplateRequest
            {
                CtrlNbr = ctrlNbr,
                Template = template
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<WorkflowTemplateDetailResponse> RestoreAsDraftAsync(long ctrlNbr, int versionNumber, string notes)
    {
        try
        {
            return await _client.RestoreAsDraftAsync(new RestoreWorkflowTemplateVersionRequest
            {
                CtrlNbr = ctrlNbr,
                VersionNumber = versionNumber,
                Notes = notes
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> DeleteAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteAsync(new DeleteWorkflowTemplateRequest
            {
                CtrlNbr = ctrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<WorkflowTemplateDetailResponse> SetEnabledAsync(long ctrlNbr, bool isEnabled)
    {
        try
        {
            return await _client.SetEnabledAsync(new SetWorkflowTemplateEnabledRequest
            {
                CtrlNbr = ctrlNbr,
                IsEnabled = isEnabled
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetWorkflowTemplateExecutionHistoryResponse> GetExecutionHistoryAsync(long ctrlNbr, int take = 100)
    {
        try
        {
            return await _client.GetExecutionHistoryAsync(new GetWorkflowTemplateExecutionHistoryRequest
            {
                CtrlNbr = ctrlNbr,
                Take = take
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetWorkflowReferenceCatalogResponse> GetReferenceCatalogAsync(long railroadCtrlNbr)
    {
        try
        {
            return await _client.GetReferenceCatalogAsync(new GetWorkflowReferenceCatalogRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
