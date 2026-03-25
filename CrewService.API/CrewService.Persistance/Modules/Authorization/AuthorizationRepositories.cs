using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Authorization;

internal sealed class RoleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Role>(dbContext, currentUserService), IRoleRepository
{
    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await DbContext.Set<Role>()
            .SingleOrDefaultAsync(r => r.Name == name, ct);
    }

    public async Task<Role?> GetByNameIncludingDeletedAsync(string name, CancellationToken ct = default)
    {
        return await DbContext.Set<Role>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(r => r.Name == name, ct);
    }
}

internal sealed class FeatureRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Feature>(dbContext, currentUserService), IFeatureRepository
{
    public async Task<Feature?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        return await DbContext.Set<Feature>()
            .SingleOrDefaultAsync(f => f.Key == key, ct);
    }

    public async Task<List<Feature>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        return await DbContext.Set<Feature>()
            .Where(f => f.Category == category)
            .OrderBy(f => f.DisplayName)
            .ToListAsync(ct);
    }
}

internal sealed class PermissionRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Permission>(dbContext, currentUserService), IPermissionRepository
{
    public async Task<Permission?> GetByRoleFeatureParentAsync(ControlNumber roleCtrlNbr, ControlNumber featureCtrlNbr, long? parentCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<Permission>()
            .SingleOrDefaultAsync(p =>
                p.RoleCtrlNbr == roleCtrlNbr &&
                p.FeatureCtrlNbr == featureCtrlNbr &&
                p.ParentCtrlNbr == parentCtrlNbr, ct);
    }

    public async Task<List<Permission>> GetByRoleCtrlNbrAsync(ControlNumber roleCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<Permission>()
            .Where(p => p.RoleCtrlNbr == roleCtrlNbr)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns the effective permissions for a role in a given parent context.
    /// For each feature, returns the parent-specific override if it exists,
    /// otherwise falls back to the global default (ParentCtrlNbr == null).
    /// </summary>
    public async Task<List<Permission>> GetEffectivePermissionsAsync(ControlNumber roleCtrlNbr, long? parentCtrlNbr, CancellationToken ct = default)
    {
        var allForRole = await DbContext.Set<Permission>()
            .Where(p => p.RoleCtrlNbr == roleCtrlNbr &&
                        (p.ParentCtrlNbr == null || p.ParentCtrlNbr == parentCtrlNbr))
            .ToListAsync(ct);

        // Group by feature, prefer parent-specific override over global default
        return allForRole
            .GroupBy(p => p.FeatureCtrlNbr)
            .Select(g => g.FirstOrDefault(p => p.ParentCtrlNbr == parentCtrlNbr) ?? g.First())
            .ToList();
    }
}
