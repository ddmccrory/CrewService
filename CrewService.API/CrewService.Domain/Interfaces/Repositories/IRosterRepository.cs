using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRosterRepository : IRepository<Roster>
{
    Task<List<Roster>> GetByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr);
}