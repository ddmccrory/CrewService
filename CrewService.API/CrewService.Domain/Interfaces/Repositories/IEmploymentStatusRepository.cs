using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmploymentStatusRepository : IRepository<EmploymentStatus>
{
    Task<List<EmploymentStatus>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr);
    Task<List<EmploymentStatus>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr, int pageNumber, int pageSize);
}