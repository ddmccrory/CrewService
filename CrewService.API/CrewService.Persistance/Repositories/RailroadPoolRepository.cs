using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RailroadPoolRepository(CrewServiceDbContext dbContext)
    : Repository<RailroadPool>(dbContext), IRailroadPoolRepository
{
    public async Task<RailroadPool?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<RailroadPool>()
            .Include(rp => rp.PayrollTiers)
            .Include(rp => rp.PoolEmployees)
            .SingleOrDefaultAsync(rp => rp.CtrlNbr == ctrlNbr);
    }

    public override async Task<RailroadPool?> GetByCtrlNbrAsync(long ctrlNbr)
    {
        if (ctrlNbr <= 0)
            throw new ArgumentException("RailroadPool control number must be greater than zero", nameof(ctrlNbr));

        return await DbContext.Set<RailroadPool>()
            .Include(rp => rp.PayrollTiers)
            .Include(rp => rp.PoolEmployees)
            .SingleOrDefaultAsync(rp => rp.CtrlNbr == ControlNumber.Create(ctrlNbr));
    }

    public async Task<List<RailroadPool>> GetByRailroadCtrlNbrAsync(long railroadCtrlNbr)
    {
        if (railroadCtrlNbr <= 0)
            throw new ArgumentException("Railroad control number must be greater than zero", nameof(railroadCtrlNbr));

        return await DbContext.Set<RailroadPool>()
            .Include(rp => rp.PayrollTiers)
            .Include(rp => rp.PoolEmployees)
            .Where(rp => rp.RailroadCtrlNbr == ControlNumber.Create(railroadCtrlNbr))
            .ToListAsync();
    }

    public async Task<List<RailroadPool>> GetAllByRailroadAsync(long railroadCtrlNbr)
    {
        return await GetByRailroadCtrlNbrAsync(railroadCtrlNbr);
    }

    public async Task<List<RailroadPool>> GetAllByRailroadAsync(ControlNumber railroadCtrlNbr)
    {
        return await DbContext.Set<RailroadPool>()
            .Include(rp => rp.PayrollTiers)
            .Include(rp => rp.PoolEmployees)
            .Where(rp => rp.RailroadCtrlNbr == railroadCtrlNbr)
            .ToListAsync();
    }

    public async Task AddAsync(RailroadPool railroadPool)
    {
        DbContext.Set<RailroadPool>().Add(railroadPool);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(RailroadPool railroadPool)
    {
        DbContext.Set<RailroadPool>().Update(railroadPool);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<RailroadPool>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}