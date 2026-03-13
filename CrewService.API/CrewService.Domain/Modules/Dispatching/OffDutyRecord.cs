using CrewService.Domain.DomainEvents.DailyOperations;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Dispatching;

public sealed class OffDutyRecord : Entity
{
    public ControlNumber OnDutyRecordCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public DateTime OffDutyTimeUtc { get; private set; }
    public int TotalTimeOnDutyMinutes { get; private set; }
    public decimal RestHoursRequired { get; private set; }
    public DateTime RestedAtUtc { get; private set; }
    public DateTime ConsecutiveDayRestedAtUtc { get; private set; }
    public string ReleaseReason { get; private set; } = string.Empty;

    private OffDutyRecord()
    {
        OnDutyRecordCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static OffDutyRecord Create(
        ControlNumber onDutyRecordCtrlNbr,
        ControlNumber employeeCtrlNbr,
        DateTime offDutyTimeUtc,
        int totalTimeOnDutyMinutes,
        decimal restHoursRequired,
        decimal consecutiveDayResetHours,
        string releaseReason)
    {
        var record = new OffDutyRecord
        {
            OnDutyRecordCtrlNbr = onDutyRecordCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            OffDutyTimeUtc = offDutyTimeUtc,
            TotalTimeOnDutyMinutes = totalTimeOnDutyMinutes,
            RestHoursRequired = restHoursRequired,
            RestedAtUtc = offDutyTimeUtc.AddHours((double)restHoursRequired),
            ConsecutiveDayRestedAtUtc = offDutyTimeUtc.AddHours((double)consecutiveDayResetHours),
            ReleaseReason = releaseReason,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
        record.Raise(new OffDutyRecordCreatedDomainEvent(record.CtrlNbr, employeeCtrlNbr, onDutyRecordCtrlNbr));
        return record;
    }
}
