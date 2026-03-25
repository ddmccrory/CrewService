using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class AuthorizationService(
    IRoleRepository roleRepository,
    IFeatureRepository featureRepository,
    IPermissionRepository permissionRepository) : AuthorizationSrvc.AuthorizationSrvcBase
{
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly IFeatureRepository _featureRepository = featureRepository;
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    // ---- Roles ----

    public override async Task<GetAllRolesResponse> GetAllRoles(GetAllRolesRequest request, ServerCallContext context)
    {
        var roles = await _roleRepository.GetAllAsync();
        var response = new GetAllRolesResponse();
        foreach (var role in roles.OrderByDescending(r => r.Level).ThenBy(r => r.Name))
            response.Roles.Add(MapRole(role));
        return response;
    }

    public override async Task<RoleResponse> GetRole(GetRoleRequest request, ServerCallContext context)
    {
        var role = await _roleRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Role {request.CtrlNbr} not found."));
        return MapRole(role);
    }

    public override async Task<RoleResponse> CreateRole(CreateRoleRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Role name is required."));

        var existing = await _roleRepository.GetByNameIncludingDeletedAsync(request.Name);
        if (existing is not null)
        {
            if (!existing.IsDeleted)
                throw new RpcException(new Status(StatusCode.AlreadyExists, $"Role '{request.Name}' already exists."));

            existing.Restore();
            existing.Update(request.Name, request.Description, request.Level);
            await _roleRepository.UpdateAsync(existing);
            return MapRole(existing);
        }

        var role = Role.Create(request.Name, request.Description, isSystem: false, request.Level);
        await _roleRepository.AddAsync(role);
        return MapRole(role);
    }

    public override async Task<RoleResponse> UpdateRole(UpdateRoleRequest request, ServerCallContext context)
    {
        var role = await _roleRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Role {request.CtrlNbr} not found."));

        if (role.IsSystem)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"System role '{role.Name}' cannot be modified."));

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Role name is required."));

        role.Update(request.Name, request.Description, request.Level);
        await _roleRepository.UpdateAsync(role);
        return MapRole(role);
    }

    public override async Task<DeleteResponse> DeleteRole(DeleteRoleRequest request, ServerCallContext context)
    {
        var role = await _roleRepository.GetByCtrlNbrAsync(ControlNumber.Create(request.CtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Role {request.CtrlNbr} not found."));

        if (role.IsSystem)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"System role '{role.Name}' cannot be deleted."));

        await _roleRepository.DeleteAsync(ControlNumber.Create(request.CtrlNbr));
        return new DeleteResponse { Success = true };
    }

    // ---- Features ----

    public override async Task<GetAllFeaturesResponse> GetAllFeatures(GetAllFeaturesRequest request, ServerCallContext context)
    {
        var features = await _featureRepository.GetAllAsync();
        var response = new GetAllFeaturesResponse();
        foreach (var feature in features.OrderBy(f => f.Category).ThenBy(f => f.DisplayName))
            response.Features.Add(MapFeature(feature));
        return response;
    }

    // ---- Permissions ----

    public override async Task<GetPermissionMatrixResponse> GetPermissionMatrix(GetPermissionMatrixRequest request, ServerCallContext context)
    {
        var roles = await _roleRepository.GetAllAsync();
        var features = await _featureRepository.GetAllAsync();

        var response = new GetPermissionMatrixResponse();

        foreach (var role in roles.OrderByDescending(r => r.Level).ThenBy(r => r.Name))
            response.Roles.Add(MapRole(role));

        foreach (var feature in features.OrderBy(f => f.Category).ThenBy(f => f.DisplayName))
            response.Features.Add(MapFeature(feature));

        // Load permissions: parent/craft-specific if requested, otherwise global defaults
        long? parentCtrlNbr = request.ParentCtrlNbr > 0 ? request.ParentCtrlNbr : null;
        ControlNumber? craftCtrlNbr = request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null;

        foreach (var role in roles)
        {
            var effective = await _permissionRepository.GetEffectivePermissionsAsync(
                role.CtrlNbr, parentCtrlNbr, craftCtrlNbr);

            foreach (var perm in effective)
                response.Permissions.Add(MapPermission(perm));
        }

        return response;
    }

    public override async Task<GetEffectivePermissionsResponse> GetEffectivePermissions(GetEffectivePermissionsRequest request, ServerCallContext context)
    {
        long? parentCtrlNbr = request.ParentCtrlNbr > 0 ? request.ParentCtrlNbr : null;
        ControlNumber? craftCtrlNbr = request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null;
        var permissions = await _permissionRepository.GetEffectivePermissionsAsync(
            ControlNumber.Create(request.RoleCtrlNbr), parentCtrlNbr, craftCtrlNbr);

        var response = new GetEffectivePermissionsResponse();
        foreach (var perm in permissions)
            response.Permissions.Add(MapPermission(perm));
        return response;
    }

    public override async Task<PermissionResponse> UpdatePermission(UpdatePermissionRequest request, ServerCallContext context)
    {
        long? parentCtrlNbr = request.ParentCtrlNbr > 0 ? request.ParentCtrlNbr : null;
        ControlNumber? craftCtrlNbr = request.CraftCtrlNbr > 0 ? ControlNumber.Create(request.CraftCtrlNbr) : null;
        var accessLevel = (AccessLevel)request.AccessLevel;

        var existing = await _permissionRepository.GetByRoleFeatureParentCraftAsync(
            ControlNumber.Create(request.RoleCtrlNbr),
            ControlNumber.Create(request.FeatureCtrlNbr),
            parentCtrlNbr,
            craftCtrlNbr);

        if (existing is not null)
        {
            existing.UpdateAccessLevel(accessLevel);
            await _permissionRepository.UpdateAsync(existing);
            return MapPermission(existing);
        }

        var permission = Permission.Create(
            ControlNumber.Create(request.RoleCtrlNbr),
            ControlNumber.Create(request.FeatureCtrlNbr),
            accessLevel,
            parentCtrlNbr,
            craftCtrlNbr);
        await _permissionRepository.AddAsync(permission);
        return MapPermission(permission);
    }

    // ---- Mappers ----

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
        ParentCtrlNbr = permission.ParentCtrlNbr ?? 0,
        CraftCtrlNbr = permission.CraftCtrlNbr?.Value ?? 0
    };
}
