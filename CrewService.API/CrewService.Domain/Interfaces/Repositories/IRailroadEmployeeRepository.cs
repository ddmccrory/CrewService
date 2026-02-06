using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadEmployeeRepository
{
    Task<List<RailroadEmployee>> GetAllAsync();
    Task<List<RailroadEmployee>> GetAllAsync(int pageNumber, int pageSize);
    Task<RailroadEmployee?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<RailroadEmployee?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<RailroadEmployee>> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr);
    Task<RailroadEmployee?> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr, long railroadCtrlNbr);
    Task<List<RailroadEmployee>> GetByRailroadCtrlNbrAsync(long railroadCtrlNbr);
    Task<List<RailroadEmployee>> GetAllByRailroadCtrlNbrAsync(long railroadCtrlNbr);

    Task AddAsync(RailroadEmployee railroadEmployee);
    Task UpdateAsync(RailroadEmployee railroadEmployee);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(RailroadEmployee railroadEmployee);
    void Update(RailroadEmployee railroadEmployee);
    void Remove(RailroadEmployee railroadEmployee);
}
