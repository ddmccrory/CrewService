using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public interface IShiftDefinitionRepository
{
    Task<IReadOnlyList<ShiftDefinition>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}

public interface IShiftInstanceRepository
{
    Task AddAsync(ShiftInstance instance, CancellationToken ct = default);
    Task<ShiftInstance?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<ShiftInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr, CancellationToken ct = default);
}

public interface IAssignmentTemplateQueryService
{
    Task<IReadOnlyList<AssignmentTemplateDto>> GetTemplatesForDateAsync(ControlNumber workAreaGroupCtrlNbr, DateOnly targetDate, CancellationToken ct = default);
}

public sealed record AssignmentTemplateDto(
    ControlNumber TemplateCtrlNbr,
    ControlNumber WorkAreaGroupCtrlNbr,
    IReadOnlyList<CrewPositionDto> Positions);

public sealed record CrewPositionDto(
    ControlNumber PositionCtrlNbr,
    ControlNumber? IncumbentEmployeeCtrlNbr,
    int DisplayOrder);
