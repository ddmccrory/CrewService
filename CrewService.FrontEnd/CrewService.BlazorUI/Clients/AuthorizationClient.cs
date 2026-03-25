using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Grpc.Core;

namespace CrewService.BlazorUI.Clients;

public sealed class AuthorizationClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, ILogger<AuthorizationClient> logger)
    : BaseGrpcClient<AuthorizationSrvc.AuthorizationSrvcClient>(channelProvider, tokenProvider, callInvoker => new AuthorizationSrvc.AuthorizationSrvcClient(callInvoker), logger)
{
    // ── Roles ───────────────────────────────────────────────────────────

    public async Task<GetAllRolesResponse> GetAllRolesAsync()
    {
        try
        {
            return await _client.GetAllRolesAsync(new GetAllRolesRequest());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<RoleResponse> GetRoleAsync(long ctrlNbr)
    {
        try
        {
            return await _client.GetRoleAsync(new GetRoleRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<RoleResponse> CreateRoleAsync(string name, string description, int level)
    {
        try
        {
            return await _client.CreateRoleAsync(new CreateRoleRequest
            {
                Name = name,
                Description = description,
                Level = level
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<RoleResponse> UpdateRoleAsync(long ctrlNbr, string name, string description, int level)
    {
        try
        {
            return await _client.UpdateRoleAsync(new UpdateRoleRequest
            {
                CtrlNbr = ctrlNbr,
                Name = name,
                Description = description,
                Level = level
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task DeleteRoleAsync(long ctrlNbr)
    {
        try
        {
            await _client.DeleteRoleAsync(new DeleteRoleRequest { CtrlNbr = ctrlNbr });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    // ── Features ────────────────────────────────────────────────────────

    public async Task<GetAllFeaturesResponse> GetAllFeaturesAsync()
    {
        try
        {
            return await _client.GetAllFeaturesAsync(new GetAllFeaturesRequest());
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    // ── Permissions ─────────────────────────────────────────────────────

    public async Task<GetPermissionMatrixResponse> GetPermissionMatrixAsync(long parentCtrlNbr = 0, long craftCtrlNbr = 0)
    {
        try
        {
            return await _client.GetPermissionMatrixAsync(new GetPermissionMatrixRequest
            {
                ParentCtrlNbr = parentCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<GetEffectivePermissionsResponse> GetEffectivePermissionsAsync(long roleCtrlNbr, long parentCtrlNbr = 0, long craftCtrlNbr = 0)
    {
        try
        {
            return await _client.GetEffectivePermissionsAsync(new GetEffectivePermissionsRequest
            {
                RoleCtrlNbr = roleCtrlNbr,
                ParentCtrlNbr = parentCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    public async Task<PermissionResponse> UpdatePermissionAsync(long roleCtrlNbr, long featureCtrlNbr, int accessLevel, long parentCtrlNbr = 0, long craftCtrlNbr = 0)
    {
        try
        {
            return await _client.UpdatePermissionAsync(new UpdatePermissionRequest
            {
                RoleCtrlNbr = roleCtrlNbr,
                FeatureCtrlNbr = featureCtrlNbr,
                AccessLevel = accessLevel,
                ParentCtrlNbr = parentCtrlNbr,
                CraftCtrlNbr = craftCtrlNbr
            });
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
