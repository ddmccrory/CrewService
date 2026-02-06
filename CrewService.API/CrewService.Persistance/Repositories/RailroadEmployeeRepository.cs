using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RailroadEmployeeRepository(CrewServiceDbContext dbContext)
    : Repository<RailroadEmployee>(dbContext), IRailroadEmployeeRepository
{
    public async Task<List<RailroadEmployee>> GetAllAsync(int pageNumber, int pageSize)
    {
        return await DbContext.Set<RailroadEmployee>()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<RailroadEmployee?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<RailroadEmployee>()
            .SingleOrDefaultAsync(re => re.CtrlNbr == ctrlNbr);
    }

    public async Task<List<RailroadEmployee>> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr)
    {
        if (employeeCtrlNbr <= 0)
            throw new ArgumentException("Employee control number must be greater than zero", nameof(employeeCtrlNbr));

        return await DbContext.Set<RailroadEmployee>()
            .Where(re => re.EmployeeCtrlNbr == ControlNumber.Create(employeeCtrlNbr))
            .ToListAsync();
    }

    public async Task<RailroadEmployee?> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr, long railroadCtrlNbr)
    {
        if (employeeCtrlNbr <= 0)
            throw new ArgumentException("Employee control number must be greater than zero", nameof(employeeCtrlNbr));
        if (railroadCtrlNbr <= 0)
            throw new ArgumentException("Railroad control number must be greater than zero", nameof(railroadCtrlNbr));

        return await DbContext.Set<RailroadEmployee>()
            .SingleOrDefaultAsync(re => re.EmployeeCtrlNbr == ControlNumber.Create(employeeCtrlNbr) 
                                     && re.RailroadCtrlNbr == ControlNumber.Create(railroadCtrlNbr));
    }

    public async Task<List<RailroadEmployee>> GetByRailroadCtrlNbrAsync(long railroadCtrlNbr)
    {
        if (railroadCtrlNbr <= 0)
            throw new ArgumentException("Railroad control number must be greater than zero", nameof(railroadCtrlNbr));

        return await DbContext.Set<RailroadEmployee>()
            .Where(re => re.RailroadCtrlNbr == ControlNumber.Create(railroadCtrlNbr))
            .ToListAsync();
    }

    public async Task<List<RailroadEmployee>> GetAllByRailroadCtrlNbrAsync(long railroadCtrlNbr)
    {
        return await GetByRailroadCtrlNbrAsync(railroadCtrlNbr);
    }

    public async Task AddAsync(RailroadEmployee railroadEmployee)
    {
        DbContext.Set<RailroadEmployee>().Add(railroadEmployee);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(RailroadEmployee railroadEmployee)
    {
        DbContext.Set<RailroadEmployee>().Update(railroadEmployee);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<RailroadEmployee>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}