using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed record DailyCallSheetDueWorkItem(
    ControlNumber WorkAreaGroupCtrlNbr,
    ControlNumber ShiftDefinitionCtrlNbr,
    DateOnly TargetDate,
    ControlNumber? DepartmentCtrlNbr);

public sealed record DailyCallSheetNextEventCandidate(
    DateTime EventUtc,
    DailyCallSheetDueWorkItem Item,
    string ShiftCode,
    string ShiftDisplayName,
    string? DepartmentName);

public interface IDailyCallSheetSchedulerService
{
    Task<DateTime?> GetNextCallSheetEventUtcAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
    Task<DailyCallSheetNextEventCandidate?> GetNextCallSheetEventCandidateAsync(ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<DailyCallSheetDueWorkItem>> GetDueWorkItemsAsync(ControlNumber workAreaGroupCtrlNbr, DateTime nowUtc, CancellationToken ct = default);
}
