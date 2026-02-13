using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface ISeniorityRepository : IRepository<Seniority>
{
    Task<List<Seniority>> GetByRosterCtrlNbrAsync(ControlNumber rosterCtrlNbr);
    Task<List<Seniority>> GetByRailroadPoolEmployeeCtrlNbrAsync(ControlNumber railroadPoolEmployeeCtrlNbr);
}