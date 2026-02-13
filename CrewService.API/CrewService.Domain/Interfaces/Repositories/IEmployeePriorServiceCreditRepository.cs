using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmployeePriorServiceCreditRepository : IRepository<EmployeePriorServiceCredit>
{
    Task<EmployeePriorServiceCredit?> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr);
}