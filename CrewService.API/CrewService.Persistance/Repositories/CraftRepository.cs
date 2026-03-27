using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class CraftRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Craft>(dbContext, currentUserService), ICraftRepository
{
    public async Task<List<Craft>> GetByParentAndRailroadAsync(long parentCtrlNbr, long? railroadCtrlNbr)
    {
        var railroadCn = railroadCtrlNbr.HasValue ? ControlNumber.Create(railroadCtrlNbr.Value) : (ControlNumber?)null;
        return await DbContext.Set<Craft>()
            .Where(c => c.ParentCtrlNbr == parentCtrlNbr
                && (c.DynamicGroupCtrlNbr == null || c.DynamicGroupCtrlNbr == railroadCn))
            .OrderBy(c => c.CraftNumber)
            .ToListAsync();
    }
}