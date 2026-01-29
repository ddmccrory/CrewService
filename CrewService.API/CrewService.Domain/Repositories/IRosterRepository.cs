using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Repositories;

public interface IRosterRepository
{
    Task<List<Roster>> GetAllAsync();
    Task<Roster?> GetByIdAsync(ControlNumber ctrlNbr);
    Task AddAsync(Roster roster);
    Task UpdateAsync(Roster roster);
    Task DeleteAsync(ControlNumber ctrlNbr);
}