using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class DepartmentClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<DepartmentClient> logger)
    : BaseGrpcClient<DepartmentSrvc.DepartmentSrvcClient>(channelProvider, tokenProvider, callInvoker => new DepartmentSrvc.DepartmentSrvcClient(callInvoker), logger)
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

    public async Task<DepartmentResponse> CreateAsync(long parentCtrlNbr, long railroadCtrlNbr, string name)
    {
        try
        {
            return await _client.CreateAsync(new CreateDepartmentRequest
            {
                ParentCtrlNbr = parentCtrlNbr,
                DynamicGroupCtrlNbr = railroadCtrlNbr,
                Name = name
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DepartmentResponse> UpdateAsync(long ctrlNbr, string name)
    {
        try
        {
            return await _client.UpdateAsync(new UpdateDepartmentRequest
            {
                CtrlNbr = ctrlNbr,
                Name = name
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
