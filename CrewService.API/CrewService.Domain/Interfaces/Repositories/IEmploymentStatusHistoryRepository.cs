using CrewService.Domain.Models.Employment;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmploymentStatusHistoryRepository
{
    Task<List<EmploymentStatusHistory>> GetAllByEmployeeAsync(long employeeCtrlNbr);
    Task<List<EmploymentStatusHistory>> GetAllByEmployeeAsync(long employeeCtrlNbr, int pageNumber, int pageSize);
    Task<EmploymentStatusHistory?> GetByCtrlNbrAsync(long ctrlNbr);
    void Add(EmploymentStatusHistory history);
    void Remove(EmploymentStatusHistory history);
}