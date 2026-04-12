using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class RosterClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<RosterClient> logger)
    : BaseGrpcClient<RosterSrvc.RosterSrvcClient>(channelProvider, tokenProvider, callInvoker => new RosterSrvc.RosterSrvcClient(callInvoker), logger)
{
    public async Task<GetAllRosterResponse> GetAllAsync(long craftCtrlNbr)
    {
        try
        {
            return await _client.GetAllAsyncAsync(new GetAllRosterRequest
            {
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
