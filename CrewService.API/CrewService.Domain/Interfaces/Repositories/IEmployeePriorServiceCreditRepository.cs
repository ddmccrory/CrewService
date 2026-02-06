using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmployeePriorServiceCreditRepository
{
    Task<List<EmployeePriorServiceCredit>> GetAllAsync();
    Task<EmployeePriorServiceCredit?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<EmployeePriorServiceCredit?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<EmployeePriorServiceCredit?> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr);

    Task AddAsync(EmployeePriorServiceCredit priorServiceCredit);
    Task UpdateAsync(EmployeePriorServiceCredit priorServiceCredit);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(EmployeePriorServiceCredit priorServiceCredit);
    void Update(EmployeePriorServiceCredit priorServiceCredit);
    void Remove(EmployeePriorServiceCredit priorServiceCredit);
}