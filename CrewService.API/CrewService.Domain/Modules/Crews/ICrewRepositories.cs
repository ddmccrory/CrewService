using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Crews;

public interface ICrewRepository : IRepository<Crew>
{
    Task<List<Crew>> GetByWorkAreaAsync(ControlNumber workAreaCtrlNbr);
    Task<List<Crew>> GetByTypeAsync(string crewType);
    Task<List<Crew>> GetByRailroadAsync(ControlNumber railroadCtrlNbr);
    Task<bool> ExistsByNameInWorkAreaAsync(ControlNumber workAreaCtrlNbr, string name, ControlNumber? excludeCtrlNbr = null);
}

public interface ICrewPositionRepository : IRepository<CrewPosition>
{
    Task<List<CrewPosition>> GetByCrewAsync(ControlNumber crewCtrlNbr);
    Task<List<CrewPosition>> GetByCrewsAsync(IEnumerable<ControlNumber> crewCtrlNbrs);
}

public interface ICrewIncumbencyRepository : IRepository<CrewIncumbency>
{
    Task<List<CrewIncumbency>> GetByCrewPositionAsync(ControlNumber crewPositionCtrlNbr);
    Task<List<CrewIncumbency>> GetActiveByEmployeeAsync(ControlNumber employeeCtrlNbr, DateTime asOfUtc);
}

public interface ICrewAttachmentInstanceRepository : IRepository<CrewAttachmentInstance>
{
    Task<List<CrewAttachmentInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr);
}

public interface ICrewAssignmentRepository : IRepository<CrewAssignment>
{
    Task<List<CrewAssignment>> GetByCrewAsync(ControlNumber crewCtrlNbr);
    Task<List<CrewAssignment>> GetByCrewsAsync(IEnumerable<ControlNumber> crewCtrlNbrs);
    Task<List<CrewAssignment>> GetByAssignmentAsync(ControlNumber assignmentCtrlNbr);
}
