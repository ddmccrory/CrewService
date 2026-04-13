using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class RosterBoardClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<RosterBoardClient> logger)
    : BaseGrpcClient<RosterBoardSrvc.RosterBoardSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new RosterBoardSrvc.RosterBoardSrvcClient(callInvoker), logger)
{
    public async Task<GetAllRosterBoardsResponse> GetAllAsync(long parentCtrlNbr, long railroadCtrlNbr = 0, long craftCtrlNbr = 0)
    {
        try
        {
            return await _client.GetAllRosterBoardsAsync(new GetAllRosterBoardsRequest
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

    public async Task<RosterBoardResponse> GetAsync(long ctrlNbr)
    {
        try
        {
            return await _client.GetRosterBoardAsync(new GetRosterBoardRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<RosterBoardResponse> CreateAsync(CreateRosterBoardRequest request)
    {
        try
        {
            return await _client.CreateRosterBoardAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<RosterBoardResponse> UpdateAsync(UpdateRosterBoardRequest request)
    {
        try
        {
            return await _client.UpdateRosterBoardAsync(request);
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
            return await _client.DeleteRosterBoardAsync(new DeleteRosterBoardRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<RosterBoardPositionResponse> AddPositionAsync(AddRosterBoardPositionRequest request)
    {
        try
        {
            return await _client.AddRosterBoardPositionAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> RemovePositionAsync(long ctrlNbr)
    {
        try
        {
            return await _client.RemoveRosterBoardPositionAsync(new RemoveRosterBoardPositionRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<RosterBoardPositionResponse> HangoutPositionAsync(long positionCtrlNbr)
    {
        try
        {
            return await _client.HangoutPositionAsync(new HangoutPositionRequest { PositionCtrlNbr = positionCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<RosterBoardPositionResponse> RestorePositionAsync(long positionCtrlNbr)
    {
        try
        {
            return await _client.RestorePositionAsync(new RestorePositionRequest { PositionCtrlNbr = positionCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<RosterBoardResponse> ReorderPositionsAsync(long rosterBoardCtrlNbr, IEnumerable<(long PositionCtrlNbr, int PositionOrder)> entries)
    {
        try
        {
            var request = new ReorderRosterBoardPositionsRequest
            {
                RosterBoardCtrlNbr = rosterBoardCtrlNbr
            };
            foreach (var (posCtrlNbr, order) in entries)
            {
                request.Entries.Add(new PositionOrderEntry
                {
                    PositionCtrlNbr = posCtrlNbr,
                    PositionOrder = order
                });
            }
            return await _client.ReorderRosterBoardPositionsAsync(request);
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
