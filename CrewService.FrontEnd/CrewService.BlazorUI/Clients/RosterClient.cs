using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class RosterClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<RosterClient> logger)
    : BaseGrpcClient<RosterSrvc.RosterSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new RosterSrvc.RosterSrvcClient(callInvoker), logger)
{
    public async Task<GetAllRosterResponse> GetAllAsync(long parentCtrlNbr, long railroadCtrlNbr = 0, long craftCtrlNbr = 0)
    {
        try
        {
            return await _client.GetAllAsyncAsync(new GetAllRosterRequest
            {
                ParentCtrlNbr = parentCtrlNbr,
                DynamicGroupCtrlNbr = railroadCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr,
                PageSize = 1000
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<RosterResponse> CreateAsync(CreateRosterRequest request)
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

    public async Task<RosterResponse> UpdateAsync(UpdateRosterRequest request)
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
            return await _client.DeleteAsyncAsync(new DeleteRosterRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
