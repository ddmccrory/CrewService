using CrewService.Domain.DomainEvents.DailyOperations;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Dispatching;

public sealed class OnDutyRecord : Entity
{
    public ControlNumber PositionSlotCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber? BookingCtrlNbr { get; private set; }
    public DateTime OnDutyTimeUtc { get; private set; }
    public DateTime ScheduledOnDutyTimeUtc { get; private set; }
    public bool IsLateCall { get; private set; }
    public DateTime? LateCallAdjustedTimeUtc { get; private set; }
    public decimal PreviousRestHours { get; private set; }
    public int ConsecutiveDays { get; private set; }
    public OnDutyStatus Status { get; private set; } = OnDutyStatus.Called;
    public OnDutyCompletionStatus CompletionStatus { get; private set; } = OnDutyCompletionStatus.NotStarted;
    public bool IsAssigned { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private OnDutyRecord()
    {
        PositionSlotCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static OnDutyRecord Create(
        ControlNumber positionSlotCtrlNbr,
        ControlNumber employeeCtrlNbr,
        DateTime onDutyTimeUtc,
        DateTime scheduledOnDutyTimeUtc,
        decimal previousRestHours,
        int consecutiveDays,
        bool isAssigned,
        int lateCallThresholdMinutes = 0)
    {
        var isLate = (onDutyTimeUtc - scheduledOnDutyTimeUtc).TotalMinutes > lateCallThresholdMinutes
                     && lateCallThresholdMinutes > 0;

        var record = new OnDutyRecord
        {
            PositionSlotCtrlNbr = positionSlotCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            OnDutyTimeUtc = onDutyTimeUtc,
            ScheduledOnDutyTimeUtc = scheduledOnDutyTimeUtc,
            IsLateCall = isLate,
            LateCallAdjustedTimeUtc = isLate ? onDutyTimeUtc.AddMinutes(90) : null,
            PreviousRestHours = previousRestHours,
            ConsecutiveDays = consecutiveDays,
            Status = OnDutyStatus.OnDuty,
            CompletionStatus = OnDutyCompletionStatus.NotStarted,
            CompletedAtUtc = null,
            IsAssigned = isAssigned
        };
        record.Raise(new OnDutyRecordCreatedDomainEvent(record.CtrlNbr, employeeCtrlNbr, positionSlotCtrlNbr));
        return record;
    }

    /// <summary>
    /// Creates a planned on-duty record for an incumbent at call-sheet generation. The employee is
    /// not yet on duty: the scheduled and actual on-duty times are identical, and the record is never
    /// a late call. <paramref name="isAssigned"/> indicates the employee is working their own assigned
    /// position (rather than covering another). The lifecycle begins at <c>"Scheduled"</c> and later
    /// transitions to <c>"OnDuty"</c> and <c>"TiedUp"</c>.
    /// </summary>
    public static OnDutyRecord CreateScheduled(
        ControlNumber positionSlotCtrlNbr,
        ControlNumber employeeCtrlNbr,
        DateTime scheduledOnDutyTimeUtc,
        decimal previousRestHours,
        int consecutiveDays,
        bool isAssigned = false)
    {
        var record = new OnDutyRecord
        {
            PositionSlotCtrlNbr = positionSlotCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            OnDutyTimeUtc = scheduledOnDutyTimeUtc,
            ScheduledOnDutyTimeUtc = scheduledOnDutyTimeUtc,
            IsLateCall = false,
            LateCallAdjustedTimeUtc = null,
            PreviousRestHours = previousRestHours,
            ConsecutiveDays = consecutiveDays,
            Status = OnDutyStatus.Scheduled,
            CompletionStatus = OnDutyCompletionStatus.NotStarted,
            CompletedAtUtc = null,
            IsAssigned = isAssigned
        };
        record.Raise(new OnDutyRecordCreatedDomainEvent(record.CtrlNbr, employeeCtrlNbr, positionSlotCtrlNbr));
        return record;
    }

    public void SetBooking(ControlNumber bookingCtrlNbr)
    {
        BookingCtrlNbr = bookingCtrlNbr;
    }

    public void TieUp(bool requiresDeferredEmployeeCompletion)
    {
        Status = OnDutyStatus.TiedUp;
        CompletionStatus = requiresDeferredEmployeeCompletion
            ? OnDutyCompletionStatus.PendingEmployeeCompletion
            : OnDutyCompletionStatus.NotStarted;
    }

    public void CompleteByEmployee()
    {
        if (Status != OnDutyStatus.TiedUp)
            throw new InvalidOperationException("On-duty record cannot be completed until tied up.");

        CompletionStatus = OnDutyCompletionStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }
}
