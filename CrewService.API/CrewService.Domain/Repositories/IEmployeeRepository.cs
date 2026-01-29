using CrewService.Domain.Models.Employees;

namespace CrewService.Domain.Repositories;

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync();
    Task<List<Employee>> GetAllAsync(int pageNumber, int pageSize);
    Task<Employee?> GetByCtrlNbrAsync(long employeeCtrlNbr);
    Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber);
    void Add(Employee employee);
    void Update(Employee employee);
    void Remove(Employee employee);
}