using CrewService.Domain.Models.Employees;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmployeePriorServiceCreditRepository
{
    Task<EmployeePriorServiceCredit?> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr);
    void Add(EmployeePriorServiceCredit credit);
    void Update(EmployeePriorServiceCredit credit);
    void Remove(EmployeePriorServiceCredit credit);
}