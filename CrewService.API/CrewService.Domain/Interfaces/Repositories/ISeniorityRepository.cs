using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface ISeniorityRepository
{
    Task<List<Seniority>> GetAllAsync();
    Task<Seniority?> GetByIdAsync(ControlNumber ctrlNbr);
    Task AddAsync(Seniority seniority);
    Task UpdateAsync(Seniority seniority);
    Task DeleteAsync(ControlNumber ctrlNbr);
}