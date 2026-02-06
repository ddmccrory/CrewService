using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadPoolRepository
{
    Task<List<RailroadPool>> GetAllAsync();
    Task<RailroadPool?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<RailroadPool?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<RailroadPool>> GetByRailroadCtrlNbrAsync(long railroadCtrlNbr);
    Task<List<RailroadPool>> GetAllByRailroadAsync(long railroadCtrlNbr);
    Task<List<RailroadPool>> GetAllByRailroadAsync(ControlNumber railroadCtrlNbr);

    Task AddAsync(RailroadPool railroadPool);
    Task UpdateAsync(RailroadPool railroadPool);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(RailroadPool railroadPool);
    void Update(RailroadPool railroadPool);
    void Remove(RailroadPool railroadPool);
}