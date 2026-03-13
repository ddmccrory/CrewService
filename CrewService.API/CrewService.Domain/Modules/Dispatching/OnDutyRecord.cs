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
    public string Status { get; private set; } = "Called";
    public bool IsAssigned { get; private set; }

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
            Status = "OnDuty",
            IsAssigned = isAssigned,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
        record.Raise(new OnDutyRecordCreatedDomainEvent(record.CtrlNbr, employeeCtrlNbr, positionSlotCtrlNbr));
        return record;
    }

    public void SetBooking(ControlNumber bookingCtrlNbr)
    {
        BookingCtrlNbr = bookingCtrlNbr;
    }

    public void TieUp()
    {
        Status = "TiedUp";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }
}
