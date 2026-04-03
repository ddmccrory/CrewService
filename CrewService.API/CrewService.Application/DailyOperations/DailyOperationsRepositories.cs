using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public interface IShiftInstanceRepository
{
    Task AddAsync(ShiftInstance instance, CancellationToken ct = default);
    Task<ShiftInstance?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<ShiftInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr, CancellationToken ct = default);
}

public interface IAssignmentQueryService
{
    Task<IReadOnlyList<AssignmentDto>> GetTemplatesForDateAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber shiftDefinitionCtrlNbr, DateOnly targetDate, CancellationToken ct = default);
}

public sealed record AssignmentDto(
    ControlNumber AssignmentCtrlNbr,
    ControlNumber WorkAreaGroupCtrlNbr,
    IReadOnlyList<CrewPositionDto> Positions);

public sealed record CrewPositionDto(
    ControlNumber PositionCtrlNbr,
    ControlNumber? IncumbentEmployeeCtrlNbr,
    int DisplayOrder);
