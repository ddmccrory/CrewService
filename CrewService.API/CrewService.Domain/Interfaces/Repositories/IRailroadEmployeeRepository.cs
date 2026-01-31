using CrewService.Domain.Models.Railroads;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadEmployeeRepository
{
    Task<List<RailroadEmployee>> GetAllAsync();
    Task<List<RailroadEmployee>> GetAllAsync(int pageNumber, int pageSize);
    Task<RailroadEmployee?> GetByCtrlNbrAsync(long railroadEmployeeCtrlNbr);
    Task<RailroadEmployee?> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr, long railroadCtrlNbr);
    Task<List<RailroadEmployee>> GetAllByRailroadCtrlNbrAsync(long railroadCtrlNbr);

    void Add(RailroadEmployee railroadEmployee);
    void Update(RailroadEmployee railroadEmployee);
    void Remove(RailroadEmployee railroadEmployee);
}
