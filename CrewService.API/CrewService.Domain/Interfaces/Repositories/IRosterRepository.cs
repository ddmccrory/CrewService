using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRosterRepository
{
    Task<List<Roster>> GetAllAsync();
    Task<Roster?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<Roster?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<Roster>> GetByCraftCtrlNbrAsync(long craftCtrlNbr);

    Task AddAsync(Roster roster);
    Task UpdateAsync(Roster roster);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(Roster roster);
    void Update(Roster roster);
    void Remove(Roster roster);
}