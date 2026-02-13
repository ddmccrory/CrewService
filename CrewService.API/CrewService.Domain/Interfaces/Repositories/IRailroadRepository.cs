using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadRepository : IRepository<Railroad>
{
    Task<List<Railroad>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr);
}
