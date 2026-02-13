using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmployeePriorServiceCreditRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EmployeePriorServiceCredit>(dbContext, currentUserService), IEmployeePriorServiceCreditRepository
{
    public async Task<EmployeePriorServiceCredit?> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
    {
        return await DbContext.Set<EmployeePriorServiceCredit>()
            .SingleOrDefaultAsync(psc => psc.EmployeeCtrlNbr == employeeCtrlNbr);
    }
}