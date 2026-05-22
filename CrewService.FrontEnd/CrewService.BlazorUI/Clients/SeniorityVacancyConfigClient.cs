using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class SeniorityVacancyConfigClient(
    GrpcChannelProvider channelProvider,
    CircuitTokenProvider tokenProvider,
    AppContextService appContext,
    ILogger<SeniorityVacancyConfigClient> logger)
    : BaseGrpcClient<SeniorityStateVacancyConfigSrvc.SeniorityStateVacancyConfigSrvcClient>(
        channelProvider, tokenProvider, appContext,
        callInvoker => new SeniorityStateVacancyConfigSrvc.SeniorityStateVacancyConfigSrvcClient(callInvoker),
        logger)
{
    public async Task<GetVacancyConfigsResponse> GetByRailroadAsync(long railroadCtrlNbr)
    {
        try
        {
            return await _client.GetByRailroadAsyncAsync(
                new GetVacancyConfigsByRailroadRequest { RailroadCtrlNbr = railroadCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<VacancyConfigResponse> UpsertAsync(UpsertVacancyConfigRequest request)
    {
        try
        {
            return await _client.UpsertAsyncAsync(request);
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
            return await _client.DeleteAsyncAsync(new DeleteVacancyConfigRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
