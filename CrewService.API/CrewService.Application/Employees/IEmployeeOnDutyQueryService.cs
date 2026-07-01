using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Employees;

/// <summary>
/// Read-model query surface that resolves the denormalized display data and work-area timezone
/// for a set of <c>PositionSlotInstance</c> rows referenced by on-duty records. This exists as a
/// dedicated query service (mirroring <see cref="DailyOperations.IAssignmentQueryService"/>)
/// because <c>PositionSlotInstance</c> is a child of the <c>ShiftInstance</c> aggregate with no
/// standalone repository, and the employee-detail on-duty surfaces need to join across
/// <c>PositionSlotInstance → ShiftInstance → WorkInstance → DynamicGroup</c> to render
/// work-area-localized times.
/// </summary>
public interface IEmployeeOnDutyQueryService
{
    /// <summary>
    /// Resolves display metadata keyed by position-slot control number. Slots that cannot be
    /// resolved (e.g. deleted) are simply omitted from the result.
    /// </summary>
    Task<IReadOnlyDictionary<ControlNumber, EmployeeOnDutySlotDisplay>> GetSlotDisplayAsync(
        IReadOnlyList<ControlNumber> positionSlotCtrlNbrs, CancellationToken ct = default);
}

/// <summary>
/// Denormalized display data for a single on-duty position slot, including the work area's
/// configured timezone id (used to localize the record's UTC on/off-duty times for display).
/// </summary>
public sealed record EmployeeOnDutySlotDisplay(
    string  AssignmentName,
    string  AssignmentCode,
    string  CrewName,
    string  CraftRoleName,
    string  Location,
    string? TimeZoneId);
