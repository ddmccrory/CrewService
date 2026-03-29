using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class WorkManagementClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<WorkManagementClient> logger)
    : BaseGrpcClient<WorkManagementSrvc.WorkManagementSrvcClient>(channelProvider, tokenProvider, callInvoker => new WorkManagementSrvc.WorkManagementSrvcClient(callInvoker), logger)
{





    // ── Position Roles ──

    public async Task<GetPositionRolesResponse> GetPositionRolesAsync(long craftCtrlNbr)
    {
        try
        {
            return await _client.GetPositionRolesAsync(new GetPositionRolesRequest { CraftCtrlNbr = craftCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<PositionRoleResponse> CreatePositionRoleAsync(long craftCtrlNbr, string? code, string name, string? alternateName)
    {
        try
        {
            return await _client.CreatePositionRoleAsync(new CreatePositionRoleRequest
            {
                CraftCtrlNbr = craftCtrlNbr,
                Code = code ?? string.Empty,
                Name = name,
                AlternateName = alternateName ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<PositionRoleResponse> UpdatePositionRoleAsync(long ctrlNbr, string? code, string name, string? alternateName)
    {
        try
        {
            return await _client.UpdatePositionRoleAsync(new UpdatePositionRoleRequest
            {
                CtrlNbr = ctrlNbr,
                Code = code ?? string.Empty,
                Name = name,
                AlternateName = alternateName ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<DeleteResponse> DeletePositionRoleAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeletePositionRoleAsync(new DeletePositionRoleRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
