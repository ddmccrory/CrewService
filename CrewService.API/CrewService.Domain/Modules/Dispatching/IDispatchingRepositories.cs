using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Dispatching;

public interface IDispatchProjectionRepository : IRepository<DispatchProjection>
{
    Task<List<DispatchProjection>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr);
}

public interface IDispatchDecisionLogRepository : IRepository<DispatchDecisionLog>
{
    Task<List<DispatchDecisionLog>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr);
}

public interface IDispatchOverrideRepository : IRepository<DispatchOverride>
{
    Task<List<DispatchOverride>> GetByPositionSlotAsync(ControlNumber positionSlotCtrlNbr);
    Task<List<DispatchOverride>> GetPendingAsync();
}

public interface IEmployeeBookingRepository : IRepository<EmployeeBooking>
{
    Task<List<EmployeeBooking>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc);
    Task<bool> HasOverlapAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc);
}

public interface IVacancyFillLogRepository : IRepository<VacancyFillLog>
{
    Task<List<VacancyFillLog>> GetByShiftAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default);
    Task<List<VacancyFillLog>> GetByWorkAreaAndDateRangeAsync(
        ControlNumber workAreaGroupCtrlNbr,
        DateTime startUtc,
        DateTime endUtc,
        ControlNumber? departmentCtrlNbr,
        CancellationToken ct = default);
}

public interface IOnDutyRecordRepository : IRepository<OnDutyRecord>
{
    Task<IReadOnlyList<OnDutyRecord>> GetRecentForEmployeeAsync(ControlNumber employeeCtrlNbr, int dayCount, CancellationToken ct = default);
    Task<IReadOnlyList<OnDutyRecord>> GetByPositionSlotsAsync(IReadOnlyList<ControlNumber> positionSlotCtrlNbrs, CancellationToken ct = default);

    /// <summary>
    /// Open on-duty records for an employee — those not yet tied up (Scheduled, Called, or OnDuty),
    /// most recent first. Mirrors the legacy "Open On Duty Records" pay-period slice.
    /// </summary>
    Task<IReadOnlyList<OnDutyRecord>> GetOpenForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// Incomplete on-duty records for an employee (anything not in completion state Completed),
    /// most recent first. Includes tied-up records awaiting employee completion.
    /// </summary>
    Task<IReadOnlyList<OnDutyRecord>> GetIncompleteForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// Not-started on-duty records scoped to a railroad's work areas, used by the manager
    /// On-Duty / Off-Duty page. Excludes deferred quick-tie-up employee-completion items.
    /// </summary>
    Task<IReadOnlyList<OnDutyRecord>> GetNotStartedForRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// On-duty records for an employee whose on-duty time falls within <paramref name="startUtc"/>
    /// (inclusive) and <paramref name="endUtc"/> (exclusive), most recent first. Backs the legacy
    /// completed pay-period history windows (current/previous work period, month, year-to-date).
    /// </summary>
    Task<IReadOnlyList<OnDutyRecord>> GetForEmployeeInRangeAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);

    /// <summary>
    /// Operational on-duty records for an employee whose on-duty time falls within
    /// <paramref name="startUtc"/> (inclusive) and <paramref name="endUtc"/> (exclusive), most
    /// recent first. Includes all on-duty statuses (scheduled/called/on-duty/tied-up) for runtime
    /// board and tie-up calculations.
    /// </summary>
    Task<IReadOnlyList<OnDutyRecord>> GetOperationalForEmployeeInRangeAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);

    /// <summary>
    /// Returns completion statuses for on-duty records that belong to a shift instance via
    /// PositionSlotInstance relationships.
    /// </summary>
    Task<IReadOnlyList<OnDutyCompletionStatus>> GetCompletionStatusesForShiftAsync(ControlNumber shiftInstanceCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// Returns tie-up context for a single on-duty record, including assignment code, shift,
    /// and work-area identifiers resolved through position-slot and work-instance joins.
    /// </summary>
    Task<OnDutyTieUpContext?> GetTieUpContextAsync(ControlNumber onDutyRecordCtrlNbr, CancellationToken ct = default);
}

public sealed record OnDutyTieUpContext(
    ControlNumber OnDutyRecordCtrlNbr,
    string AssignmentCode,
    ControlNumber ShiftInstanceCtrlNbr,
    ControlNumber WorkAreaCtrlNbr);

public interface IOffDutyRecordRepository : IRepository<OffDutyRecord>
{
    Task<OffDutyRecord?> GetLastForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// Off-duty (tie-up) records keyed by the on-duty records they close, for enriching history rows
    /// with off-duty time and total time on duty.
    /// </summary>
    Task<IReadOnlyList<OffDutyRecord>> GetByOnDutyRecordsAsync(IReadOnlyList<ControlNumber> onDutyRecordCtrlNbrs, CancellationToken ct = default);
}
