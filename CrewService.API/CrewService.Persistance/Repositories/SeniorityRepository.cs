using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class SeniorityRepository(CrewServiceDbContext dbContext)
    : Repository<Seniority>(dbContext), ISeniorityRepository
{
    public async Task<Seniority?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<Seniority>()
            .SingleOrDefaultAsync(s => s.CtrlNbr == ctrlNbr);
    }

    public async Task<List<Seniority>> GetByRosterCtrlNbrAsync(long rosterCtrlNbr)
    {
        if (rosterCtrlNbr <= 0)
            throw new ArgumentException("Roster control number must be greater than zero", nameof(rosterCtrlNbr));

        return await DbContext.Set<Seniority>()
            .Where(s => s.RosterCtrlNbr == ControlNumber.Create(rosterCtrlNbr))
            .OrderBy(s => s.Rank)
            .ToListAsync();
    }

    public async Task<List<Seniority>> GetByRailroadPoolEmployeeCtrlNbrAsync(long railroadPoolEmployeeCtrlNbr)
    {
        if (railroadPoolEmployeeCtrlNbr <= 0)
            throw new ArgumentException("RailroadPoolEmployee control number must be greater than zero", nameof(railroadPoolEmployeeCtrlNbr));

        return await DbContext.Set<Seniority>()
            .Where(s => s.RailroadPoolEmployeeCtrlNbr == ControlNumber.Create(railroadPoolEmployeeCtrlNbr))
            .OrderByDescending(s => s.RosterDate)
            .ToListAsync();
    }

    public async Task AddAsync(Seniority seniority)
    {
        DbContext.Set<Seniority>().Add(seniority);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Seniority seniority)
    {
        DbContext.Set<Seniority>().Update(seniority);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<Seniority>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}