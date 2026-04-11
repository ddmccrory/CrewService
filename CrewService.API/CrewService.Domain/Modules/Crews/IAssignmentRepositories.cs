using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Crews;

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<List<Assignment>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr);
    Task<List<Assignment>> GetByWorkAreaAndDepartmentAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber departmentCtrlNbr);
    Task<List<Assignment>> GetAllByRailroadAsync(ControlNumber railroadCtrlNbr);
    Task<bool> ExistsByCodeInWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, string code, ControlNumber? excludeCtrlNbr = null);
}

public interface IAssignmentScheduleRepository : IRepository<AssignmentSchedule>
{
    Task<List<AssignmentSchedule>> GetByAssignmentAsync(ControlNumber assignmentCtrlNbr);
    Task<List<AssignmentSchedule>> GetByAssignmentsAsync(IEnumerable<ControlNumber> assignmentCtrlNbrs);
    Task<List<AssignmentSchedule>> GetByShiftDefinitionAsync(ControlNumber shiftDefinitionCtrlNbr);
}
