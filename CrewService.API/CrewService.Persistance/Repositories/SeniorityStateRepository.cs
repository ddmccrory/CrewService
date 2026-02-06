using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class SeniorityStateRepository(CrewServiceDbContext dbContext)
    : Repository<SeniorityState>(dbContext), ISeniorityStateRepository
{
    public async Task<List<SeniorityState>> GetAllAsync(int pageNumber, int pageSize)
    {
        return await DbContext.Set<SeniorityState>()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<SeniorityState?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<SeniorityState>()
            .SingleOrDefaultAsync(ss => ss.CtrlNbr == ctrlNbr);
    }

    public async Task AddAsync(SeniorityState seniorityState)
    {
        DbContext.Set<SeniorityState>().Add(seniorityState);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(SeniorityState seniorityState)
    {
        DbContext.Set<SeniorityState>().Update(seniorityState);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<SeniorityState>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}