using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadPoolRepository : IRepository<RailroadPool>
{
    Task<List<RailroadPool>> GetByRailroadCtrlNbrAsync(ControlNumber railroadCtrlNbr);
}