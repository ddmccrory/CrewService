using CrewService.BlazorUI.Services;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Clients;

public sealed class BootstrapClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<BootstrapClient> logger)
    : BaseGrpcClient<BootstrapSrvc.BootstrapSrvcClient>(channelProvider, tokenProvider, callInvoker => new BootstrapSrvc.BootstrapSrvcClient(callInvoker), logger)
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
