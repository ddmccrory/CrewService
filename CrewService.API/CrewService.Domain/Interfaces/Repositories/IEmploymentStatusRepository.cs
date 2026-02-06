using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmploymentStatusRepository
{
    Task<List<EmploymentStatus>> GetAllAsync();
    Task<List<EmploymentStatus>> GetAllAsync(long clientCtrlNbr);
    Task<List<EmploymentStatus>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize);
    Task<EmploymentStatus?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<EmploymentStatus?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<EmploymentStatus>> GetByClientCtrlNbrAsync(long clientCtrlNbr);

    Task AddAsync(EmploymentStatus employmentStatus);
    Task UpdateAsync(EmploymentStatus employmentStatus);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(EmploymentStatus employmentStatus);
    void Update(EmploymentStatus employmentStatus);
    void Remove(EmploymentStatus employmentStatus);
}