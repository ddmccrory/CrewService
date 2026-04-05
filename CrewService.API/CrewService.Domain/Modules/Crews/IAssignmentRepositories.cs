using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Crews;

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<List<Assignment>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr);
    Task<List<Assignment>> GetByWorkAreaAndDepartmentAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber departmentCtrlNbr);
    Task<List<Assignment>> GetAllByRailroadAsync(ControlNumber railroadCtrlNbr);
}

public interface IAssignmentScheduleRepository : IRepository<AssignmentSchedule>
{
    Task<List<AssignmentSchedule>> GetByAssignmentAsync(ControlNumber assignmentCtrlNbr);
    Task<List<AssignmentSchedule>> GetByShiftDefinitionAsync(ControlNumber shiftDefinitionCtrlNbr);
}
