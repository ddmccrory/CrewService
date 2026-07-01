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
    /// On-duty records for an employee whose on-duty time falls within <paramref name="startUtc"/>
    /// (inclusive) and <paramref name="endUtc"/> (exclusive), most recent first. Backs the legacy
    /// completed pay-period history windows (current/previous work period, month, year-to-date).
    /// </summary>
    Task<IReadOnlyList<OnDutyRecord>> GetForEmployeeInRangeAsync(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
}

public interface IOffDutyRecordRepository : IRepository<OffDutyRecord>
{
    Task<OffDutyRecord?> GetLastForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// Off-duty (tie-up) records keyed by the on-duty records they close, for enriching history rows
    /// with off-duty time and total time on duty.
    /// </summary>
    Task<IReadOnlyList<OffDutyRecord>> GetByOnDutyRecordsAsync(IReadOnlyList<ControlNumber> onDutyRecordCtrlNbrs, CancellationToken ct = default);
}
