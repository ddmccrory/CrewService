using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface ICraftRepository : IRepository<Craft>
{
    Task<List<Craft>> GetByRailroadPoolCtrlNbrAsync(ControlNumber railroadPoolCtrlNbr);
}