using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class CraftClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<CraftClient> logger)
    : BaseGrpcClient<CraftSrvc.CraftSrvcClient>(channelProvider, tokenProvider, callInvoker => new CraftSrvc.CraftSrvcClient(callInvoker), logger)
{
    public async Task<GetAllCraftResponse> GetAllCraftsAsync(long dynamicGroupCtrlNbr = 0)
    {
        try
        {
            return await _client.GetAllAsyncAsync(new GetAllCraftRequest
            {
                DynamicGroupCtrlNbr = dynamicGroupCtrlNbr,
                PageSize = 1000
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
