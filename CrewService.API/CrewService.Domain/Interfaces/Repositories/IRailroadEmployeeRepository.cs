using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadEmployeeRepository : IRepository<RailroadEmployee>
{
    Task<List<RailroadEmployee>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr);
    Task<RailroadEmployee?> GetByEmployeeAndRailroadAsync(ControlNumber employeeCtrlNbr, ControlNumber railroadCtrlNbr);
    Task<List<RailroadEmployee>> GetByRailroadCtrlNbrAsync(ControlNumber railroadCtrlNbr);
}
