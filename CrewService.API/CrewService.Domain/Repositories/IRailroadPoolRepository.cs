using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Repositories;

public interface IRailroadPoolRepository
{
    Task<RailroadPool?> GetByIdAsync(ControlNumber ctrlNbr, CancellationToken cancellationToken = default);
    Task AddAsync(RailroadPool railroadPool, CancellationToken cancellationToken = default);
    Task UpdateAsync(RailroadPool railroadPool, CancellationToken cancellationToken = default);
    Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RailroadPool>> GetAllByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken cancellationToken = default);
}