using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadPoolEmployeeRepository : IRepository<RailroadPoolEmployee>
{
    Task<List<RailroadPoolEmployee>> GetByRailroadPoolCtrlNbrAsync(ControlNumber railroadPoolCtrlNbr);
    Task<List<RailroadPoolEmployee>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr);
}