using CrewService.Domain.Models.Employment;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmploymentStatusRepository
{
    Task<List<EmploymentStatus>> GetAllAsync(long clientCtrlNbr);
    Task<List<EmploymentStatus>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize);
    Task<EmploymentStatus?> GetByCtrlNbrAsync(long ctrlNbr);
    void Add(EmploymentStatus status);
    void Update(EmploymentStatus status);
    void Remove(EmploymentStatus status);
}