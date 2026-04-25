using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class AuthorizationService(IServiceProvider serviceProvider) : AuthorizationSrvc.AuthorizationSrvcBase
{
    public override async Task<GetAllRolesResponse> GetAllRoles(GetAllRolesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Authorization.AuthorizationService>();
        var roles = await svc.GetAllRolesAsync(context.CancellationToken);
        var response = new GetAllRolesResponse();
        foreach (var role in roles.OrderByDescending(r => r.Level).ThenBy(r => r.Name))
            response.Roles.Add(MapRole(role));
        return response;
    }

    public override async Task<RoleResponse> GetRole(GetRoleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Authorization.AuthorizationService>();
        try { return MapRole(await svc.GetRoleAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<RoleResponse> CreateRole(CreateRoleRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Role name is required."));
        var svc = serviceProvider.GetRequiredService<Application.Authorization.AuthorizationService>();
        try { return MapRole(await svc.CreateRoleAsync(request.Name, request.Description, request.Level, context.CancellationToken)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message)); }
    }

    public override async Task<RoleResponse> UpdateRole(UpdateRoleRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Role name is required."));
        var svc = serviceProvider.GetRequiredService<Application.Authorization.AuthorizationService>();
        try { return MapRole(await svc.UpdateRoleAsync(ControlNumber.Create(request.CtrlNbr), request.Name, request.Description, request.Level, context.CancellationToken)); }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<DeleteResponse> DeleteRole(DeleteRoleRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Authorization.AuthorizationService>();
        try
        {
            await svc.DeleteRoleAsync(ControlNumber.Create(request.CtrlNbr), context.CancellationToken);
            return new DeleteResponse { Success = true };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<GetAllFeaturesResponse> GetAllFeatures(GetAllFeaturesRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Authorization.AuthorizationService>();
        var features = await svc.GetAllFeaturesAsync(context.CancellationToken);
        var response = new GetAllFeaturesResponse();
        foreach (var feature in features.OrderBy(f => f.Category).ThenBy(f => f.DisplayName))
            response.Features.Add(MapFeature(feature));
        return response;
    }

    public override async Task<GetPermissionMatrixResponse> GetPermissionMatrix(GetPermissionMatrixRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Authorization.AuthorizationService>();
        ControlNumber? parentCtrlNbr = request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null;
        ControlNumber? craftCtrlNbr = request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null;
        var (roles, features, permsByRole) = await svc.GetPermissionMatrixAsync(parentCtrlNbr, craftCtrlNbr, context.CancellationToken);

        var response = new GetPermissionMatrixResponse();
        foreach (var role in roles.OrderByDescending(r => r.Level).ThenBy(r => r.Name))
            response.Roles.Add(MapRole(role));
        foreach (var feature in features.OrderBy(f => f.Category).ThenBy(f => f.DisplayName))
            response.Features.Add(MapFeature(feature));
        foreach (var kvp in permsByRole)
            foreach (var perm in kvp.Value)
                response.Permissions.Add(MapPermission(perm));
        return response;
    }

    public override async Task<GetEffectivePermissionsResponse> GetEffectivePermissions(GetEffectivePermissionsRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Authorization.AuthorizationService>();
        IEnumerable<long> roleCtrlNbrs = request.RoleCtrlNbrs.Count > 0 ? request.RoleCtrlNbrs : [request.RoleCtrlNbr];
        ControlNumber? parentCtrlNbr = request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null;
        ControlNumber? craftCtrlNbr = request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null;

        var permissions = await svc.GetEffectivePermissionsAsync(
            roleCtrlNbrs.Select(ControlNumber.Create), parentCtrlNbr, craftCtrlNbr, context.CancellationToken);
        var response = new GetEffectivePermissionsResponse();
        foreach (var perm in permissions) response.Permissions.Add(MapPermission(perm));
        return response;
    }

    public override async Task<PermissionResponse> UpdatePermission(UpdatePermissionRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Authorization.AuthorizationService>();
        ControlNumber? parentCtrlNbr = request.ParentCtrlNbr > 0 ? ControlNumber.Create(request.ParentCtrlNbr) : null;
        ControlNumber? craftCtrlNbr = request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null;
        var permission = await svc.UpdatePermissionAsync(
            ControlNumber.Create(request.RoleCtrlNbr), ControlNumber.Create(request.FeatureCtrlNbr),
            parentCtrlNbr, craftCtrlNbr, request.AccessLevel, context.CancellationToken);
        return MapPermission(permission);
    }

    private static RoleResponse MapRole(Role role) => new()
    {
        CtrlNbr = role.CtrlNbr.Value,
        Name = role.Name,
        Description = role.Description ?? string.Empty,
        IsSystem = role.IsSystem,
        Level = role.Level
    };

    private static FeatureResponse MapFeature(Feature feature) => new()
    {
        CtrlNbr = feature.CtrlNbr.Value,
        Key = feature.Key,
        DisplayName = feature.DisplayName,
        Category = feature.Category,
        Route = feature.Route
    };

    private static PermissionResponse MapPermission(Permission permission) => new()
    {
        CtrlNbr = permission.CtrlNbr.Value,
        RoleCtrlNbr = permission.RoleCtrlNbr.Value,
        FeatureCtrlNbr = permission.FeatureCtrlNbr.Value,
        AccessLevel = (int)permission.AccessLevel,
        ParentCtrlNbr = permission.ParentCtrlNbr?.Value ?? 0,
        CraftCtrlNbr = permission.CraftCtrlNbr?.Value ?? 0
    };
}
