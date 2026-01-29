using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Repositories;

public interface IRailroadPoolEmployeeRepository
{
    Task<RailroadPoolEmployee?> GetByIdAsync(ControlNumber ctrlNbr, CancellationToken cancellationToken = default);
    Task AddAsync(RailroadPoolEmployee employee, CancellationToken cancellationToken = default);
    Task UpdateAsync(RailroadPoolEmployee employee, CancellationToken cancellationToken = default);
    Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RailroadPoolEmployee>> GetAllByPoolAsync(ControlNumber railroadPoolCtrlNbr, CancellationToken cancellationToken = default);
}