using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

internal sealed class EmploymentStatusClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<EmploymentStatusClient> logger)
    : BaseGrpcClient<EmploymentStatusSrvc.EmploymentStatusSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new EmploymentStatusSrvc.EmploymentStatusSrvcClient(callInvoker), logger)
{
    public async Task<GetAllEmploymentStatusResponse> GetAllAsync(long clientCtrlNbr)
    {
        try
        {
            return await _client.GetAllAsyncAsync(new GetAllEmploymentStatusRequest { ClientCtrlNbr = clientCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
