using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadPoolPayrollTierRepository
{
    Task<RailroadPoolPayrollTier?> GetByIdAsync(ControlNumber ctrlNbr, CancellationToken cancellationToken = default);
    Task AddAsync(RailroadPoolPayrollTier payrollTier, CancellationToken cancellationToken = default);
    Task UpdateAsync(RailroadPoolPayrollTier payrollTier, CancellationToken cancellationToken = default);
    Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RailroadPoolPayrollTier>> GetAllByPoolAsync(ControlNumber railroadPoolCtrlNbr, CancellationToken cancellationToken = default);
}