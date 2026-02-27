using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class RailroadsClient(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<RailroadsClient> logger)
    : BaseGrpcClient<RailroadSrvc.RailroadSrvcClient>(configuration, httpContextAccessor, callInvoker => new RailroadSrvc.RailroadSrvcClient(callInvoker), logger)
{
    public async Task<GetAllRailroadsResponse> GetAllAsync()
    {
        try
        {
            return await _client.GetAllRailroadsAsyncAsync(new GetAllRailroadsRequest());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetAllParentRailroadsResponse> GetByParentAsync(long parentCtrlNbr)
    {
        try
        {
            return await _client.GetAllParentRailroadsAsyncAsync(new GetAllParentRailroadsRequest { ParentCtrlNbr = parentCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetRailroadResponse> GetByCtrlNbrAsync(long ctrlNbr)
    {
        try
        {
            return await _client.GetRailroadAsyncAsync(new GetRailroadRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CreateRailroadResponse> CreateAsync(long parentCtrlNbr, string rrMark, string name)
    {
        try
        {
            return await _client.CreateRailroadAsyncAsync(new CreateRailroadRequest
            {
                ParentCtrlNbr = parentCtrlNbr,
                RrMark = rrMark,
                Name = name
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<UpdateRailroadResponse> UpdateAsync(long ctrlNbr, long parentCtrlNbr, string rrMark, string name)
    {
        try
        {
            return await _client.UpdateRailroadAsyncAsync(new UpdateRailroadRequest
            {
                CtrlNbr = ctrlNbr,
                ParentCtrlNbr = parentCtrlNbr,
                RrMark = rrMark,
                Name = name
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteRailroadResponse> DeleteAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteRailroadAsyncAsync(new DeleteRailroadRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
