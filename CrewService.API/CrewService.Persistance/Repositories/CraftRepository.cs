using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class CraftRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Craft>(dbContext, currentUserService), ICraftRepository
{
    public async Task<List<Craft>> GetByRailroadPoolCtrlNbrAsync(ControlNumber railroadPoolCtrlNbr)
    {
        return await DbContext.Set<Craft>()
            .Where(c => c.RailroadPoolCtrlNbr == railroadPoolCtrlNbr)
            .OrderBy(c => c.CraftNumber)
            .ToListAsync();
    }
}