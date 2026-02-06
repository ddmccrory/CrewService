using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmploymentStatusHistoryRepository(CrewServiceDbContext dbContext)
    : Repository<EmploymentStatusHistory>(dbContext), IEmploymentStatusHistoryRepository
{
    public async Task<EmploymentStatusHistory?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<EmploymentStatusHistory>()
            .SingleOrDefaultAsync(esh => esh.CtrlNbr == ctrlNbr);
    }

    public async Task<List<EmploymentStatusHistory>> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr)
    {
        if (employeeCtrlNbr <= 0)
            throw new ArgumentException("Employee control number must be greater than zero", nameof(employeeCtrlNbr));

        return await DbContext.Set<EmploymentStatusHistory>()
            .Where(esh => esh.EmployeeCtrlNbr == ControlNumber.Create(employeeCtrlNbr))
            .OrderByDescending(esh => esh.StatusChangeDate)
            .ToListAsync();
    }

    public async Task<List<EmploymentStatusHistory>> GetAllByEmployeeAsync(long employeeCtrlNbr)
    {
        return await GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
    }

    public async Task<List<EmploymentStatusHistory>> GetAllByEmployeeAsync(long employeeCtrlNbr, int pageNumber, int pageSize)
    {
        if (employeeCtrlNbr <= 0)
            throw new ArgumentException("Employee control number must be greater than zero", nameof(employeeCtrlNbr));

        return await DbContext.Set<EmploymentStatusHistory>()
            .Where(esh => esh.EmployeeCtrlNbr == ControlNumber.Create(employeeCtrlNbr))
            .OrderByDescending(esh => esh.StatusChangeDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task AddAsync(EmploymentStatusHistory employmentStatusHistory)
    {
        DbContext.Set<EmploymentStatusHistory>().Add(employmentStatusHistory);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<EmploymentStatusHistory>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}