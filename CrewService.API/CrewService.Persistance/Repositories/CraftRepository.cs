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
    public async Task<List<Craft>> GetByParentAndRailroadAsync(ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr)
    {
        return await DbContext.Set<Craft>()
            .Where(c => c.ParentCtrlNbr == parentCtrlNbr
                && (c.DynamicGroupCtrlNbr == null || c.DynamicGroupCtrlNbr == railroadCtrlNbr))
            .OrderBy(c => c.CraftNumber)
            .ToListAsync();
    }

    public async Task<List<Craft>> GetByCtrlNbrsAsync(IEnumerable<ControlNumber> ctrlNbrs)
    {
        var ids = ctrlNbrs.ToList();
        return await DbContext.Set<Craft>()
            .Where(c => ids.Contains(c.CtrlNbr))
            .ToListAsync();
    }
}