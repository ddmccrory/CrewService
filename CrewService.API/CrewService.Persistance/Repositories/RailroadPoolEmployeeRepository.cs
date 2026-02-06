using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RailroadPoolEmployeeRepository(CrewServiceDbContext dbContext)
    : Repository<RailroadPoolEmployee>(dbContext), IRailroadPoolEmployeeRepository
{
    public async Task<RailroadPoolEmployee?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<RailroadPoolEmployee>()
            .SingleOrDefaultAsync(rpe => rpe.CtrlNbr == ctrlNbr);
    }

    public async Task<List<RailroadPoolEmployee>> GetByRailroadPoolCtrlNbrAsync(long railroadPoolCtrlNbr)
    {
        if (railroadPoolCtrlNbr <= 0)
            throw new ArgumentException("RailroadPool control number must be greater than zero", nameof(railroadPoolCtrlNbr));

        return await DbContext.Set<RailroadPoolEmployee>()
            .Where(rpe => rpe.RailroadPoolCtrlNbr == ControlNumber.Create(railroadPoolCtrlNbr))
            .ToListAsync();
    }

    public async Task<List<RailroadPoolEmployee>> GetAllByPoolAsync(long railroadPoolCtrlNbr)
    {
        return await GetByRailroadPoolCtrlNbrAsync(railroadPoolCtrlNbr);
    }

    public async Task<List<RailroadPoolEmployee>> GetAllByPoolAsync(ControlNumber railroadPoolCtrlNbr)
    {
        return await DbContext.Set<RailroadPoolEmployee>()
            .Where(rpe => rpe.RailroadPoolCtrlNbr == railroadPoolCtrlNbr)
            .ToListAsync();
    }

    public async Task<List<RailroadPoolEmployee>> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr)
    {
        if (employeeCtrlNbr <= 0)
            throw new ArgumentException("Employee control number must be greater than zero", nameof(employeeCtrlNbr));

        return await DbContext.Set<RailroadPoolEmployee>()
            .Where(rpe => rpe.EmployeeCtrlNbr == ControlNumber.Create(employeeCtrlNbr))
            .ToListAsync();
    }

    public async Task AddAsync(RailroadPoolEmployee railroadPoolEmployee)
    {
        DbContext.Set<RailroadPoolEmployee>().Add(railroadPoolEmployee);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(RailroadPoolEmployee railroadPoolEmployee)
    {
        DbContext.Set<RailroadPoolEmployee>().Update(railroadPoolEmployee);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<RailroadPoolEmployee>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}