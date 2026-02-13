using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmploymentStatusHistoryRepository : IRepository<EmploymentStatusHistory>
{
    Task<List<EmploymentStatusHistory>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr);
    Task<List<EmploymentStatusHistory>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr, int pageNumber, int pageSize);
}