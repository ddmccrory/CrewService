using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public interface IShiftInstanceRepository
{
    Task AddAsync(ShiftInstance instance, CancellationToken ct = default);
    Task<ShiftInstance?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<ShiftInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr, CancellationToken ct = default);
    Task<bool> ExistsByWorkInstanceAndShiftCodeAsync(ControlNumber workInstanceCtrlNbr, string shiftCode, ControlNumber? departmentCtrlNbr, CancellationToken ct = default);
    Task UpdateAsync(ShiftInstance instance, CancellationToken ct = default);
    Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
}

public interface IAssignmentQueryService
{
    Task<IReadOnlyList<AssignmentDto>> GetTemplatesForDateAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftDefinitionCtrlNbr, DateOnly targetDate, ControlNumber? departmentCtrlNbr = null, CancellationToken ct = default);
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
    string CraftRoleName);
