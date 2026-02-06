using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class CraftRepository(CrewServiceDbContext dbContext)
    : Repository<Craft>(dbContext), ICraftRepository
{
    public async Task<Craft?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<Craft>()
            .SingleOrDefaultAsync(c => c.CtrlNbr == ctrlNbr);
    }

    public async Task<List<Craft>> GetByRailroadPoolCtrlNbrAsync(long railroadPoolCtrlNbr)
    {
        if (railroadPoolCtrlNbr <= 0)
            throw new ArgumentException("RailroadPool control number must be greater than zero", nameof(railroadPoolCtrlNbr));

        return await DbContext.Set<Craft>()
            .Where(c => c.RailroadPoolCtrlNbr == ControlNumber.Create(railroadPoolCtrlNbr))
            .OrderBy(c => c.CraftNumber)
            .ToListAsync();
    }

    public async Task AddAsync(Craft craft)
    {
        DbContext.Set<Craft>().Add(craft);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Craft craft)
    {
        DbContext.Set<Craft>().Update(craft);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<Craft>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}