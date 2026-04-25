using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Authorization;

public sealed class AuthorizationService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Roles.GetAllAsync(ct);
    }

    public async Task<Role> GetRoleAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Roles.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Role {ctrlNbr} not found.");
    }

    public async Task<Role> CreateRoleAsync(string name, string description, int level, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.Roles.GetByNameIncludingDeletedAsync(name, ct);
        if (existing is not null)
        {
            if (!existing.IsDeleted)
                throw new InvalidOperationException($"Role '{name}' already exists.");
            existing.Restore();
            existing.Update(name, description, level);
            await uow.Roles.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var role = Role.Create(name, description, isSystem: false, level);
        await uow.Roles.AddAsync(role, ct);
        await uow.CommitAsync(ct);
        return role;
    }

    public async Task<Role> UpdateRoleAsync(ControlNumber ctrlNbr, string name, string description, int level, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var role = await uow.Roles.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Role {ctrlNbr} not found.");
        if (role.IsSystem) throw new InvalidOperationException($"System role '{role.Name}' cannot be modified.");
        role.Update(name, description, level);
        await uow.Roles.UpdateAsync(role, ct);
        await uow.CommitAsync(ct);
        return role;
    }

    public async Task DeleteRoleAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var role = await uow.Roles.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Role {ctrlNbr} not found.");
        if (role.IsSystem) throw new InvalidOperationException($"System role '{role.Name}' cannot be deleted.");
        await uow.Roles.DeleteAsync(ctrlNbr, ct);
        await uow.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<Feature>> GetAllFeaturesAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Features.GetAllAsync(ct);
    }

    public async Task<(IReadOnlyList<Role> Roles, IReadOnlyList<Feature> Features,
        Dictionary<long, IReadOnlyList<Permission>> PermissionsByRole)>
        GetPermissionMatrixAsync(ControlNumber? parentCtrlNbr, ControlNumber? craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var roles = await uow.Roles.GetAllAsync(ct);
        var features = await uow.Features.GetAllAsync(ct);
        var permsByRole = new Dictionary<long, IReadOnlyList<Permission>>();
        foreach (var role in roles)
        {
            var perms = await uow.Permissions.GetEffectivePermissionsAsync(role.CtrlNbr, parentCtrlNbr, craftCtrlNbr, ct);
            permsByRole[role.CtrlNbr.Value] = perms;
        }
        return (roles, features, permsByRole);
    }

    public async Task<IReadOnlyList<Permission>> GetEffectivePermissionsAsync(
        IEnumerable<ControlNumber> roleCtrlNbrs, ControlNumber? parentCtrlNbr, ControlNumber? craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var all = new List<Permission>();
        foreach (var roleCtrlNbr in roleCtrlNbrs)
        {
            var perms = await uow.Permissions.GetEffectivePermissionsAsync(roleCtrlNbr, parentCtrlNbr, craftCtrlNbr, ct);
            all.AddRange(perms);
        }
        return all;
    }

    public async Task<Permission> UpdatePermissionAsync(
        ControlNumber roleCtrlNbr, ControlNumber featureCtrlNbr,
        ControlNumber? parentCtrlNbr, ControlNumber? craftCtrlNbr, int accessLevel, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.Permissions.GetByRoleFeatureParentCraftAsync(roleCtrlNbr, featureCtrlNbr, parentCtrlNbr, craftCtrlNbr, ct);
        if (existing is not null)
        {
            existing.UpdateAccessLevel((AccessLevel)accessLevel);
            await uow.Permissions.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var permission = Permission.Create(roleCtrlNbr, featureCtrlNbr, (AccessLevel)accessLevel, parentCtrlNbr, craftCtrlNbr);
        await uow.Permissions.AddAsync(permission, ct);
        await uow.CommitAsync(ct);
        return permission;
    }
}
