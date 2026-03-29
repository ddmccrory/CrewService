using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class CraftClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<CraftClient> logger)
    : BaseGrpcClient<CraftSrvc.CraftSrvcClient>(channelProvider, tokenProvider, callInvoker => new CraftSrvc.CraftSrvcClient(callInvoker), logger)
{
    public async Task<GetAllCraftResponse> GetAllCraftsAsync(long parentCtrlNbr, long railroadCtrlNbr = 0)
    {
        try
        {
            return await _client.GetAllAsyncAsync(new GetAllCraftRequest
            {
                ParentCtrlNbr = parentCtrlNbr,
                DynamicGroupCtrlNbr = railroadCtrlNbr,
                PageSize = 1000
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CraftResponse> CreateAsync(CreateCraftRequest request)
    {
        try
        {
            return await _client.CreateAsyncAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CraftResponse> UpdateAsync(UpdateCraftRequest request)
    {
        try
        {
            return await _client.UpdateAsyncAsync(request);
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
            return await _client.DeleteAsyncAsync(new DeleteCraftRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
