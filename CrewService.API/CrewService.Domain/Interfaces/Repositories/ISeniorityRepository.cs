using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface ISeniorityRepository
{
    Task<List<Seniority>> GetAllAsync();
    Task<Seniority?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<Seniority?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<Seniority>> GetByRosterCtrlNbrAsync(long rosterCtrlNbr);
    Task<List<Seniority>> GetByRailroadPoolEmployeeCtrlNbrAsync(long railroadPoolEmployeeCtrlNbr);

    Task AddAsync(Seniority seniority);
    Task UpdateAsync(Seniority seniority);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(Seniority seniority);
    void Update(Seniority seniority);
    void Remove(Seniority seniority);
}