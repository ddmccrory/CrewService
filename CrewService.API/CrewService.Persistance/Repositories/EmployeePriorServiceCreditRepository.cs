using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmployeePriorServiceCreditRepository(CrewServiceDbContext dbContext)
    : Repository<EmployeePriorServiceCredit>(dbContext), IEmployeePriorServiceCreditRepository
{
    public async Task<EmployeePriorServiceCredit?> GetByIdAsync(ControlNumber ctrlNbr)
    {
        return await DbContext.Set<EmployeePriorServiceCredit>()
            .SingleOrDefaultAsync(psc => psc.CtrlNbr == ctrlNbr);
    }

    public async Task<EmployeePriorServiceCredit?> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr)
    {
        if (employeeCtrlNbr <= 0)
            throw new ArgumentException("Employee control number must be greater than zero", nameof(employeeCtrlNbr));

        return await DbContext.Set<EmployeePriorServiceCredit>()
            .SingleOrDefaultAsync(psc => psc.EmployeeCtrlNbr == ControlNumber.Create(employeeCtrlNbr));
    }

    public async Task AddAsync(EmployeePriorServiceCredit priorServiceCredit)
    {
        DbContext.Set<EmployeePriorServiceCredit>().Add(priorServiceCredit);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(EmployeePriorServiceCredit priorServiceCredit)
    {
        DbContext.Set<EmployeePriorServiceCredit>().Update(priorServiceCredit);
        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr)
    {
        var entity = await GetByIdAsync(ctrlNbr);
        if (entity is not null)
        {
            DbContext.Set<EmployeePriorServiceCredit>().Remove(entity);
            await DbContext.SaveChangesAsync();
        }
    }
}