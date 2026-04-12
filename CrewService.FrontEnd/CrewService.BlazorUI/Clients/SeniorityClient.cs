using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class SeniorityClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<SeniorityClient> logger)
    : BaseGrpcClient<SenioritySrvc.SenioritySrvcClient>(channelProvider, tokenProvider, callInvoker => new SenioritySrvc.SenioritySrvcClient(callInvoker), logger)
{
    public async Task<GetAllSeniorityResponse> GetAllByRosterAsync(long rosterCtrlNbr)
    {
        try
        {
            return await _client.GetAllAsyncAsync(new GetAllSeniorityRequest
            {
                RosterCtrlNbr = rosterCtrlNbr,
                PageSize = 1000
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<SeniorityResponse> CreateAsync(CreateSeniorityRequest request)
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

    public async Task<SeniorityResponse> UpdateAsync(UpdateSeniorityRequest request)
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
            return await _client.DeleteAsyncAsync(new DeleteSeniorityRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<ActiveCraftResponse> GetActiveCraftAsync(long employeeCtrlNbr)
    {
        try
        {
            return await _client.GetActiveCraftForEmployeeAsync(new GetActiveCraftRequest
            {
                EmployeeCtrlNbr = employeeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
