using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public interface IAssignmentQueryService
{
    Task<IReadOnlyList<AssignmentDto>> GetTemplatesForDateAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftDefinitionCtrlNbr, DateOnly targetDate, ControlNumber? departmentCtrlNbr = null, CancellationToken ct = default);
    Task<IReadOnlyList<AssignmentDto>> GetExtraAssignmentsForShiftAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftDefinitionCtrlNbr, DateOnly targetDate, ControlNumber? departmentCtrlNbr = null, CancellationToken ct = default);
}

public sealed record AssignmentDto(
    ControlNumber AssignmentCtrlNbr,
    ControlNumber GroupCtrlNbr,
    ControlNumber? DepartmentCtrlNbr,
    string AssignmentCode,
    string AssignmentName,
    TimeOnly OnDutyTime,
    TimeOnly OffDutyTime,
    string GroupName,
    string GroupCode,
    IReadOnlyList<CrewPositionDto> Positions);

public sealed record CrewPositionDto(
    ControlNumber PositionCtrlNbr,
    ControlNumber? IncumbentEmployeeCtrlNbr,
    int DisplayOrder,
    string CraftRoleName,
    string CrewName = "",
    string CrewType = "REGULAR");
