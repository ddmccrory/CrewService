using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IRailroadPoolEmployeeRepository
{
    Task<List<RailroadPoolEmployee>> GetAllAsync();
    Task<RailroadPoolEmployee?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<RailroadPoolEmployee?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<RailroadPoolEmployee>> GetByRailroadPoolCtrlNbrAsync(long railroadPoolCtrlNbr);
    Task<List<RailroadPoolEmployee>> GetAllByPoolAsync(long railroadPoolCtrlNbr);
    Task<List<RailroadPoolEmployee>> GetAllByPoolAsync(ControlNumber railroadPoolCtrlNbr);
    Task<List<RailroadPoolEmployee>> GetByEmployeeCtrlNbrAsync(long employeeCtrlNbr);

    Task AddAsync(RailroadPoolEmployee railroadPoolEmployee);
    Task UpdateAsync(RailroadPoolEmployee railroadPoolEmployee);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(RailroadPoolEmployee railroadPoolEmployee);
    void Update(RailroadPoolEmployee railroadPoolEmployee);
    void Remove(RailroadPoolEmployee railroadPoolEmployee);
}