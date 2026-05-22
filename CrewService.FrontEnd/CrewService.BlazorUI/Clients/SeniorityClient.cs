using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class SeniorityClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<SeniorityClient> logger)
    : BaseGrpcClient<SenioritySrvc.SenioritySrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new SenioritySrvc.SenioritySrvcClient(callInvoker), logger)
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

    public async Task<PendingStateChangeResponse> ScheduleStateChangeAsync(
        long seniorityCtrlNbr, long toStateCtrlNbr, DateTime effectiveDateUtc)
    {
        try
        {
            return await _client.ScheduleStateChangeAsyncAsync(new ScheduleStateChangeRequest
            {
                SeniorityCtrlNbr = seniorityCtrlNbr,
                ToStateCtrlNbr = toStateCtrlNbr,
                EffectiveDateUtc = effectiveDateUtc.ToString("O")
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<PendingStateChangeResponse> GetPendingStateChangeAsync(long seniorityCtrlNbr)
    {
        try
        {
            return await _client.GetPendingStateChangeAsyncAsync(new GetPendingStateChangeRequest
            {
                SeniorityCtrlNbr = seniorityCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> CancelPendingStateChangeAsync(long pendingChangeCtrlNbr)
    {
        try
        {
            return await _client.CancelPendingStateChangeAsyncAsync(new CancelPendingStateChangeRequest
            {
                PendingChangeCtrlNbr = pendingChangeCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetAllPendingStateChangesResponse> GetAllPendingStateChangesAsync(long railroadCtrlNbr)
    {
        try
        {
            return await _client.GetAllPendingStateChangesAsyncAsync(new GetAllPendingStateChangesRequest
            {
                RailroadCtrlNbr = railroadCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetNextStateChangeEventResponse?> GetNextStateChangeEventAsync(long railroadCtrlNbr)
    {
        try
        {
            return await _client.GetNextStateChangeEventAsyncAsync(new GetNextStateChangeEventRequest { RailroadCtrlNbr = railroadCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
