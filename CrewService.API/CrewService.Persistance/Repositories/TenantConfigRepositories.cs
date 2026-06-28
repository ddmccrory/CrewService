using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class GroupTypeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<GroupType>(dbContext, currentUserService), IGroupTypeRepository
{
    public async Task<GroupType?> GetByNameAsync(string name, ControlNumber? parentCtrlNbr = null)
    {
        return await DbContext.Set<GroupType>()
            .SingleOrDefaultAsync(g => g.Name == name && g.ParentCtrlNbr == parentCtrlNbr);
    }

    public async Task<GroupType?> GetByNameIncludingDeletedAsync(string name, ControlNumber? parentCtrlNbr = null)
    {
        return await DbContext.Set<GroupType>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(g => g.Name == name && g.ParentCtrlNbr == parentCtrlNbr);
    }
}

internal sealed class DynamicGroupRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DynamicGroup>(dbContext, currentUserService), IDynamicGroupRepository
{
    public async Task<List<DynamicGroup>> GetByParentCtrlNbrAsync(ControlNumber? parentGroupCtrlNbr)
    {
        return await DbContext.Set<DynamicGroup>()
            .Where(g => g.ParentGroupCtrlNbr == parentGroupCtrlNbr)
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<List<DynamicGroup>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs)
    {
        var list = ctrlNbrs.ToList();
        return await DbContext.Set<DynamicGroup>()
            .Where(g => list.Contains(g.CtrlNbr))
            .ToListAsync();
    }

    public async Task<DynamicGroup?> GetByGroupTypeAndNameIncludingDeletedAsync(ControlNumber groupTypeCtrlNbr, string name)
    {
        return await DbContext.Set<DynamicGroup>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(g => g.GroupTypeCtrlNbr == groupTypeCtrlNbr && g.Name == name);
    }

    public async Task<List<DynamicGroup>> GetWorkAreasAsync(ControlNumber? railroadCtrlNbr = null)
    {
        var query = DbContext.Set<DynamicGroup>().Where(g => g.IsWorkArea);

        if (railroadCtrlNbr is not null)
            query = query.Where(g => g.CtrlNbr == railroadCtrlNbr || g.RailroadCtrlNbr == railroadCtrlNbr);

        return await query.OrderBy(g => g.Name).ToListAsync();
    }

    public async Task<List<DynamicGroup>> GetWorkAreasWithDescendantsAsync()
    {
        var workAreas = await DbContext.Set<DynamicGroup>()
            .Where(g => g.IsWorkArea)
            .ToListAsync();

        if (workAreas.Count == 0)
            return [];

        // Build a predicate that matches the work area itself OR any group whose path
        // starts with the work area's path + "/" (descendants).
        var workAreaPaths = workAreas
            .Where(wa => wa.Path is not null)
            .Select(wa => wa.Path!)
            .ToList();

        var allGroups = await DbContext.Set<DynamicGroup>()
            .Where(g =>
                g.IsWorkArea
                || (g.Path != null && workAreaPaths.Any(p => g.Path.StartsWith(p + "/")))
            )
            .OrderBy(g => g.Path ?? g.Name)
            .ToListAsync();

        return allGroups;
    }

    public async Task<List<DynamicGroup>> GetAncestorsAsync(ControlNumber groupCtrlNbr)
    {
        var ancestors = new List<DynamicGroup>();
        var current = await DbContext.Set<DynamicGroup>().SingleOrDefaultAsync(g => g.CtrlNbr == groupCtrlNbr);

        while (current?.ParentGroupCtrlNbr is not null)
        {
            current = await DbContext.Set<DynamicGroup>().SingleOrDefaultAsync(g => g.CtrlNbr == current.ParentGroupCtrlNbr);
            if (current is not null)
                ancestors.Add(current);
        }

        return ancestors;
    }

    public async Task<List<DynamicGroup>> GetTreeAsync(ControlNumber? rootCtrlNbr = null)
    {
        if (rootCtrlNbr is null)
            return await DbContext.Set<DynamicGroup>().OrderBy(g => g.Path).ToListAsync();

        var root = await DbContext.Set<DynamicGroup>().SingleOrDefaultAsync(g => g.CtrlNbr == rootCtrlNbr);
        if (root is null)
            return [];

        // BFS via ParentGroupCtrlNbr — works regardless of whether materialized paths
        // have been populated, so mixed-state trees are handled correctly.
        // still returned even when materialized paths haven't been backfilled.
        var result = new List<DynamicGroup> { root };
        var queue = new Queue<ControlNumber>();
        queue.Enqueue(root.CtrlNbr);

        while (queue.Count > 0)
        {
            var parentCtrlNbr = queue.Dequeue();
            var children = await DbContext.Set<DynamicGroup>()
                .Where(g => g.ParentGroupCtrlNbr == parentCtrlNbr)
                .OrderBy(g => g.Name)
                .ToListAsync();

            foreach (var child in children)
            {
                result.Add(child);
                queue.Enqueue(child.CtrlNbr);
            }
        }

        return result;
    }

    public async Task<List<DynamicGroup>> GetByGroupTypeNameAsync(string typeName, ControlNumber? parentCtrlNbr = null)
    {
        var groupTypeCtrlNbrs = await DbContext.Set<GroupType>()
            .Where(gt => gt.Name == typeName
                && (parentCtrlNbr == null || gt.ParentCtrlNbr == parentCtrlNbr || gt.ParentCtrlNbr == null))
            .Select(gt => gt.CtrlNbr)
            .ToListAsync();

        return await DbContext.Set<DynamicGroup>()
            .Where(g => groupTypeCtrlNbrs.Contains(g.GroupTypeCtrlNbr))
            .OrderBy(g => g.Name)
            .ToListAsync();
    }
    public async Task BackfillPathsAsync()
    {
        var allGroups = await DbContext.Set<DynamicGroup>().ToListAsync();
        var needsFix = allGroups.Any(g => g.Path is null);
        if (!needsFix) return;

        // BFS from root groups (no parent) outward
        var lookup = allGroups.ToLookup(g => g.ParentGroupCtrlNbr);

        var queue = new Queue<(DynamicGroup Group, string? ParentPath)>();
        foreach (var root in lookup[null])
        {
            queue.Enqueue((root, null));
        }

        while (queue.Count > 0)
        {
            var (current, parentPath) = queue.Dequeue();
            current.BuildPath(parentPath);

            foreach (var child in lookup[current.CtrlNbr])
            {
                queue.Enqueue((child, current.Path));
            }
        }

        await DbContext.SaveChangesAsync();
    }
}

internal sealed class GroupAttributeDefinitionRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<GroupAttributeDefinition>(dbContext, currentUserService), IGroupAttributeDefinitionRepository
{
    public async Task<List<GroupAttributeDefinition>> GetByGroupTypeCtrlNbrAsync(ControlNumber groupTypeCtrlNbr)
    {
        return await DbContext.Set<GroupAttributeDefinition>()
            .Where(a => a.GroupTypeCtrlNbr == groupTypeCtrlNbr)
            .OrderBy(a => a.AttributeName)
            .ToListAsync();
    }

    public async Task<GroupAttributeDefinition?> GetByGroupTypeAndAttributeNameIncludingDeletedAsync(ControlNumber groupTypeCtrlNbr, string attributeName)
    {
        return await DbContext.Set<GroupAttributeDefinition>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(a => a.GroupTypeCtrlNbr == groupTypeCtrlNbr && a.AttributeName == attributeName);
    }
}

internal sealed class GroupAttributeValueRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<GroupAttributeValue>(dbContext, currentUserService), IGroupAttributeValueRepository
{
    public async Task<List<GroupAttributeValue>> GetByGroupCtrlNbrAsync(ControlNumber groupCtrlNbr)
    {
        return await DbContext.Set<GroupAttributeValue>()
            .Where(v => v.GroupCtrlNbr == groupCtrlNbr)
            .ToListAsync();
    }
}
