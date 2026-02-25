using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.TenantConfig;

internal sealed class GroupTypeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<GroupType>(dbContext, currentUserService), IGroupTypeRepository
{
    public async Task<GroupType?> GetByNameAsync(string name)
    {
        return await DbContext.Set<GroupType>()
            .SingleOrDefaultAsync(g => g.Name == name);
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

    public async Task<List<DynamicGroup>> GetWorkAreasAsync()
    {
        return await DbContext.Set<DynamicGroup>()
            .Where(g => g.IsWorkArea)
            .OrderBy(g => g.Name)
            .ToListAsync();
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
        if (root?.Path is null)
            return root is null ? [] : [root];

        return await DbContext.Set<DynamicGroup>()
            .Where(g => g.Path != null && g.Path.StartsWith(root.Path))
            .OrderBy(g => g.Path)
            .ToListAsync();
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
