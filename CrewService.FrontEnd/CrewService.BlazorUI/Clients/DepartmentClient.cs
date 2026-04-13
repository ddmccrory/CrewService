using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class DepartmentClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, AppContextService appContext, ILogger<DepartmentClient> logger)
    : BaseGrpcClient<DepartmentSrvc.DepartmentSrvcClient>(channelProvider, tokenProvider, appContext, callInvoker => new DepartmentSrvc.DepartmentSrvcClient(callInvoker), logger)
{
    public async Task<GetDepartmentsResponse> GetAllAsync(long parentCtrlNbr, long railroadCtrlNbr)
    {
        try
        {
            return await _client.GetAllAsync(new GetDepartmentsRequest { ParentCtrlNbr = parentCtrlNbr, DynamicGroupCtrlNbr = railroadCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DepartmentResponse> CreateAsync(long parentCtrlNbr, long railroadCtrlNbr, string name, string defaultCallSheetView = "Vertical")
    {
        try
        {
            return await _client.CreateAsync(new CreateDepartmentRequest
            {
                ParentCtrlNbr = parentCtrlNbr,
                DynamicGroupCtrlNbr = railroadCtrlNbr,
                Name = name,
                DefaultCallSheetView = defaultCallSheetView
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DepartmentResponse> UpdateAsync(long ctrlNbr, string name, string defaultCallSheetView)
    {
        try
        {
            return await _client.UpdateAsync(new UpdateDepartmentRequest
            {
                CtrlNbr = ctrlNbr,
                Name = name,
                DefaultCallSheetView = defaultCallSheetView
            });
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
            return await _client.DeleteAsync(new DeleteDepartmentRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
