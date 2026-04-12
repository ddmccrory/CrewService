using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class SeniorityStateClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<SeniorityStateClient> logger)
    : BaseGrpcClient<SeniorityStateSrvc.SeniorityStateSrvcClient>(channelProvider, tokenProvider, callInvoker => new SeniorityStateSrvc.SeniorityStateSrvcClient(callInvoker), logger)
{
    public async Task<GetAllSeniorityStateResponse> GetAllAsync(long parentCtrlNbr)
    {
        try
        {
            return await _client.GetAllAsyncAsync(new GetAllSeniorityStateRequest
            {
                ParentCtrlNbr = parentCtrlNbr,
                PageSize = 1000
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<SeniorityStateResponse> CreateAsync(CreateSeniorityStateRequest request)
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

    public async Task<SeniorityStateResponse> UpdateAsync(UpdateSeniorityStateRequest request)
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
            return await _client.DeleteAsyncAsync(new DeleteSeniorityStateRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
