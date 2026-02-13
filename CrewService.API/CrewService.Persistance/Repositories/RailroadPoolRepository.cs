using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RailroadPoolRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RailroadPool>(dbContext, currentUserService), IRailroadPoolRepository
{
    public override async Task<RailroadPool?> GetByCtrlNbrAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<RailroadPool>()
            .Include(rp => rp.PayrollTiers)
            .Include(rp => rp.PoolEmployees)
            .SingleOrDefaultAsync(rp => rp.CtrlNbr == ctrlNbr);
    }

    public async Task<List<RailroadPool>> GetByRailroadCtrlNbrAsync(ControlNumber railroadCtrlNbr)
    {
        return await DbContext.Set<RailroadPool>()
            .Include(rp => rp.PayrollTiers)
            .Include(rp => rp.PoolEmployees)
            .Where(rp => rp.RailroadCtrlNbr == railroadCtrlNbr)
            .ToListAsync();
    }
}