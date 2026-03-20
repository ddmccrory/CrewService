using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class ParentsClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<ParentsClient> logger)
: BaseGrpcClient<ParentSrvc.ParentSrvcClient>(channelProvider, tokenProvider, callInvoker => new ParentSrvc.ParentSrvcClient(callInvoker), logger)
{
    public async Task<GetAllParentsResponse> GetAllAsync()
    {
        try
        {
            return await _client.GetAllParentsAsyncAsync(new GetAllParentsRequest());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetParentResponse> GetByCtrlNbrAsync(long ctrlNbr)
    {
        try
        {
            return await _client.GetParentAsyncAsync(new GetParentRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CreateParentResponse> CreateAsync(string name)
    {
        try
        {
            return await _client.CreateParentAsyncAsync(new CreateParentRequest { Name = name });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<UpdateParentResponse> UpdateAsync(long ctrlNbr, string name)
    {
        try
        {
            return await _client.UpdateParentAsyncAsync(new UpdateParentRequest { CtrlNbr = ctrlNbr, Name = name });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteParentResponse> DeleteAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteParentAsyncAsync(new DeleteParentRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
