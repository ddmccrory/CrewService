using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmploymentStatusHistoryRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EmploymentStatusHistory>(dbContext, currentUserService), IEmploymentStatusHistoryRepository
{
    public async Task<List<EmploymentStatusHistory>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
    {
        return await DbContext.Set<EmploymentStatusHistory>()
            .Where(esh => esh.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(esh => esh.StatusChangeDate)
            .ToListAsync();
    }

    public async Task<List<EmploymentStatusHistory>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, int pageNumber, int pageSize)
    {
        return await DbContext.Set<EmploymentStatusHistory>()
            .Where(esh => esh.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(esh => esh.StatusChangeDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<EmploymentStatusHistory>> GetAllByEmployeeAsync(ControlNumber employeeCtrlNbr)
    {
        return await GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
    }

    public async Task<List<EmploymentStatusHistory>> GetAllByEmployeeAsync(ControlNumber employeeCtrlNbr, int pageNumber, int pageSize)
    {
        if (employeeCtrlNbr.Value <= 0)
            throw new ArgumentException("Employee control number must be greater than zero", nameof(employeeCtrlNbr));

        return await DbContext.Set<EmploymentStatusHistory>()
            .Where(esh => esh.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(esh => esh.StatusChangeDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}