using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface ICraftRepository
{
    Task<List<Craft>> GetAllAsync();
    Task<Craft?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<Craft?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<Craft>> GetByRailroadPoolCtrlNbrAsync(long railroadPoolCtrlNbr);

    Task AddAsync(Craft craft);
    Task UpdateAsync(Craft craft);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(Craft craft);
    void Update(Craft craft);
    void Remove(Craft craft);
}