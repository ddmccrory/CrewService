using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmploymentStatusHistoryRepository
{
    Task<List<EmploymentStatusHistory>> GetAllAsync();
    Task<EmploymentStatusHistory?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<EmploymentStatusHistory?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<EmploymentStatusHistory>> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr);
    Task<List<EmploymentStatusHistory>> GetAllByEmployeeAsync(long employeeCtrlNbr);
    Task<List<EmploymentStatusHistory>> GetAllByEmployeeAsync(long employeeCtrlNbr, int pageNumber, int pageSize);

    Task AddAsync(EmploymentStatusHistory employmentStatusHistory);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(EmploymentStatusHistory employmentStatusHistory);
    void Remove(EmploymentStatusHistory employmentStatusHistory);
}