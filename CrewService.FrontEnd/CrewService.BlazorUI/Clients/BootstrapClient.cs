using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class BootstrapClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<BootstrapClient> logger)
    : BaseGrpcClient<BootstrapSrvc.BootstrapSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new BootstrapSrvc.BootstrapSrvcClient(callInvoker), logger)
{
    public async Task<BootstrapResponse> GetBootstrapDataAsync()
    {
        try
        {
            return await _client.GetBootstrapDataAsync(new BootstrapRequest());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetContextOptionsResponse> GetContextOptionsAsync()
    {
        try
        {
            return await _client.GetContextOptionsAsync(new GetContextOptionsRequest());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
