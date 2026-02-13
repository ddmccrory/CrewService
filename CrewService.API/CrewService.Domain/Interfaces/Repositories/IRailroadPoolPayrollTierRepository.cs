using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadPoolPayrollTierRepository : IRepository<RailroadPoolPayrollTier>
{
    Task<List<RailroadPoolPayrollTier>> GetByRailroadPoolCtrlNbrAsync(ControlNumber railroadPoolCtrlNbr);
}