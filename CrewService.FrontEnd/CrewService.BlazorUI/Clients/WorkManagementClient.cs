using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class WorkManagementClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<WorkManagementClient> logger)
    : BaseGrpcClient<WorkManagementSrvc.WorkManagementSrvcClient>(channelProvider, tokenProvider, callInvoker => new WorkManagementSrvc.WorkManagementSrvcClient(callInvoker), logger)
{





    // ── Craft Roles ──

    public async Task<GetCraftRolesResponse> GetCraftRolesAsync(long craftCtrlNbr = 0)
    {
        try
        {
            return await _client.GetCraftRolesAsync(new GetCraftRolesRequest { CraftCtrlNbr = craftCtrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<CraftRoleResponse> CreateCraftRoleAsync(long craftCtrlNbr, string? code, string name, string? alternateName)
    {
        try
        {
            return await _client.CreateCraftRoleAsync(new CreateCraftRoleRequest
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

    public async Task<CraftRoleResponse> UpdateCraftRoleAsync(long ctrlNbr, string? code, string name, string? alternateName)
    {
        try
        {
            return await _client.UpdateCraftRoleAsync(new UpdateCraftRoleRequest
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

    public async Task<DeleteResponse> DeleteCraftRoleAsync(long ctrlNbr)
    {
        try
        {
            return await _client.DeleteCraftRoleAsync(new DeleteCraftRoleRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
