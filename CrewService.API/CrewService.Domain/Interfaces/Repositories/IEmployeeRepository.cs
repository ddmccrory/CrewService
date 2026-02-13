using CrewService.Domain.Models.Employees;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber);
}