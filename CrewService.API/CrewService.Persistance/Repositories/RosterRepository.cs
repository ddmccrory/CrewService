using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RosterRepository(CrewServiceDbContext dbContext)
    : Repository<Roster>(dbContext), IRosterRepository
{
    public async Task<Roster?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<Roster>()
            .SingleOrDefaultAsync(r => r.CtrlNbr == ctrlNbr);
    }

    public async Task<List<Roster>> GetByCraftCtrlNbrAsync(long craftCtrlNbr)
    {
        if (craftCtrlNbr <= 0)
            throw new ArgumentException("Craft control number must be greater than zero", nameof(craftCtrlNbr));

        return await DbContext.Set<Roster>()
            .Where(r => r.CraftCtrlNbr == ControlNumber.Create(craftCtrlNbr))
            .OrderBy(r => r.RosterNumber)
            .ToListAsync();
    }

    public async Task AddAsync(Roster roster)
    {
        DbContext.Set<Roster>().Add(roster);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Roster roster)
    {
        DbContext.Set<Roster>().Update(roster);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<Roster>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}