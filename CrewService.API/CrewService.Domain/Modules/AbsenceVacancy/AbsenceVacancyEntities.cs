using CrewService.Domain.DomainEvents;
using CrewService.Domain.DomainEvents.Absence;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.AbsenceVacancy;

public sealed class AbsenceRequest : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public DateTime ScheduledStartUtc { get; private set; }
    public DateTime? ScheduledEndUtc { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public ControlNumber? ApprovedByCtrlNbr { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public ControlNumber? DeniedByCtrlNbr { get; private set; }
    public DateTime? DeniedAtUtc { get; private set; }
    public ControlNumber? CancelledByCtrlNbr { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public ControlNumber? AbsenceCodeCtrlNbr { get; private set; }
    public bool IsSystemGenerated { get; private set; }
    public bool AutoMarkOffOnApproval { get; private set; }

    private readonly List<AbsenceStartRecord> _startRecords = [];
    private readonly List<AbsenceEndRecord> _endRecords = [];
    public IReadOnlyList<AbsenceStartRecord> StartRecords => _startRecords.AsReadOnly();
    public IReadOnlyList<AbsenceEndRecord> EndRecords => _endRecords.AsReadOnly();

    public string DerivedStatus => ComputeDerivedStatus();

    private AbsenceRequest() { EmployeeCtrlNbr = null!; }

    public static AbsenceRequest Create(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc, string reasonCode, string? notes = null)
    {
        var scheduledStartUtc = AsUtc(startUtc);
        DateTime? scheduledEndUtc = endUtc.HasValue ? AsUtc(endUtc.Value) : null;
        ValidateScheduledWindow(scheduledStartUtc, scheduledEndUtc);

        var request = new AbsenceRequest
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            ScheduledStartUtc = scheduledStartUtc,
            ScheduledEndUtc = scheduledEndUtc,
            ReasonCode = reasonCode,
            Notes = notes
        };

        request.Raise(new AbsenceRequestedDomainEvent(request));
        return request;
    }

    public static AbsenceRequest CreateWithCode(
        ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc,
        ControlNumber absenceCodeCtrlNbr, string reasonCode,
        bool isSystemGenerated = false, string? notes = null,
        bool autoMarkOffOnApproval = false)
    {
        var scheduledStartUtc = AsUtc(startUtc);
        DateTime? scheduledEndUtc = endUtc.HasValue ? AsUtc(endUtc.Value) : null;
        ValidateScheduledWindow(scheduledStartUtc, scheduledEndUtc);

        var request = new AbsenceRequest
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            ScheduledStartUtc = scheduledStartUtc,
            ScheduledEndUtc = scheduledEndUtc,
            ReasonCode = reasonCode,
            AbsenceCodeCtrlNbr = absenceCodeCtrlNbr,
            IsSystemGenerated = isSystemGenerated,
            AutoMarkOffOnApproval = autoMarkOffOnApproval,
            Notes = notes
        };

        request.Raise(new AbsenceRequestedDomainEvent(request));
        return request;
    }

    public void Approve(ControlNumber approvedByCtrlNbr)
    {
        EnsureNotClosedForLifecycleChange();
        if (ApprovedAtUtc.HasValue)
            throw new InvalidOperationException("An absence request can only be approved once.");

        ApprovedByCtrlNbr = approvedByCtrlNbr;
        ApprovedAtUtc = DateTime.UtcNow;
        Raise(new AbsenceApprovedDomainEvent(this));
    }

    public void Deny(ControlNumber deniedByCtrlNbr)
    {
        EnsureNotClosedForLifecycleChange();
        if (_startRecords.Count > 0 || _endRecords.Count > 0)
            throw new InvalidOperationException("Cannot deny an absence request after it has started or ended.");

        DeniedByCtrlNbr = deniedByCtrlNbr;
        DeniedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(ControlNumber? cancelledByCtrlNbr = null)
    {
        if (_startRecords.Count > 0 || _endRecords.Count > 0)
            throw new InvalidOperationException("Cannot cancel an absence request after it has started or ended.");

        CancelledByCtrlNbr = cancelledByCtrlNbr;
        CancelledAtUtc = DateTime.UtcNow;
    }

    public void Complete(DateTime actualEndUtc, bool isAutoEndRecord = false)
    {
        AddEndRecord(actualEndUtc, isAutoEndRecord);
        Raise(new AbsenceCompletedByMarkUpDomainEvent(this));
    }

    public void ScheduleEnd(DateTime scheduledEndUtc)
    {
        EnsureNotClosedForLifecycleChange();
        if (_startRecords.Count == 0)
            throw new InvalidOperationException("Cannot schedule an absence end before it has started.");
        if (_endRecords.Count > 0)
            throw new InvalidOperationException("Cannot schedule an absence end after it has ended.");

        var endUtc = AsUtc(scheduledEndUtc);
        var startUtc = _startRecords[0].ActualStartUtc;
        if (endUtc < startUtc)
            throw new InvalidOperationException("Scheduled end time cannot be before actual start time.");

        ScheduledEndUtc = endUtc;
    }

    public void Start(DateTime actualStartUtc)
    {
        EnsureNotClosedForLifecycleChange();
        if (!ApprovedAtUtc.HasValue)
            throw new InvalidOperationException("Cannot start an absence request without approval.");
        if (_startRecords.Count > 0)
            throw new InvalidOperationException("An absence request can only have one start record.");

        var startUtc = AsUtc(actualStartUtc);
        var actualStart = startUtc < ScheduledStartUtc ? ScheduledStartUtc : startUtc;

        _startRecords.Add(AbsenceStartRecord.Create(CtrlNbr, actualStart));
    }

    public AbsenceStartRecord AddStartRecord(DateTime actualStartUtc)
    {
        Start(actualStartUtc);
        return _startRecords[0];
    }

    public AbsenceEndRecord AddEndRecord(DateTime actualEndUtc, bool isAutoEndRecord)
    {
        EnsureNotClosedForLifecycleChange();
        if (_startRecords.Count == 0)
            throw new InvalidOperationException("Cannot end an absence request before it has started.");
        if (_endRecords.Count > 0)
            throw new InvalidOperationException("An absence request can only have one end record.");

        var endUtc = AsUtc(actualEndUtc);
        var startUtc = _startRecords[0].ActualStartUtc;
        if (endUtc < startUtc)
            throw new InvalidOperationException("Actual end time cannot be before actual start time.");

        var endRecord = AbsenceEndRecord.Create(CtrlNbr, endUtc, isAutoEndRecord);
        _endRecords.Add(endRecord);
        return endRecord;
    }

    public void Exercise(DateTime exercisedUtc)
    {
        Start(exercisedUtc);
    }

    public void CompleteByMarkUp(DateTime markUpUtc)
    {
        Complete(markUpUtc, isAutoEndRecord: true);
    }

    public void SetAutoMarkOffOnApproval(bool enabled)
    {
        EnsureNotClosedForLifecycleChange();
        AutoMarkOffOnApproval = enabled;
    }

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static void ValidateScheduledWindow(DateTime scheduledStartUtc, DateTime? scheduledEndUtc)
    {
        if (scheduledEndUtc.HasValue && scheduledEndUtc.Value < scheduledStartUtc)
            throw new InvalidOperationException("Scheduled end time cannot be before scheduled start time.");
    }

    private void EnsureNotClosedForLifecycleChange()
    {
        if (DeniedAtUtc.HasValue)
            throw new InvalidOperationException("Cannot modify a denied absence request.");
        if (CancelledAtUtc.HasValue)
            throw new InvalidOperationException("Cannot modify a cancelled absence request.");
    }

    private string ComputeDerivedStatus()
    {
        if (DeniedAtUtc.HasValue)
            return "DENIED";

        if (CancelledAtUtc.HasValue)
            return "CANCELLED";

        if (!ApprovedAtUtc.HasValue)
            return "PENDING";

        if (_startRecords.Count == 0)
            return "APPROVED";

        return _endRecords.Count == 0
            ? "OPEN"
            : "COMPLETE";
    }
}

public sealed class AbsenceStartRecord : Entity
{
    public ControlNumber AbsenceRequestCtrlNbr { get; private set; }
    public DateTime ActualStartUtc { get; private set; }

    private AbsenceStartRecord() { AbsenceRequestCtrlNbr = null!; }

    internal static AbsenceStartRecord Create(ControlNumber absenceRequestCtrlNbr, DateTime actualStartUtc)
    {
        return new AbsenceStartRecord
        {
            AbsenceRequestCtrlNbr = absenceRequestCtrlNbr,
            ActualStartUtc = actualStartUtc
        };
    }
}

public sealed class AbsenceEndRecord : Entity
{
    public ControlNumber AbsenceRequestCtrlNbr { get; private set; }
    public DateTime ActualEndUtc { get; private set; }
    public bool IsAutoEndRecord { get; private set; }

    private AbsenceEndRecord() { AbsenceRequestCtrlNbr = null!; }

    internal static AbsenceEndRecord Create(ControlNumber absenceRequestCtrlNbr, DateTime actualEndUtc, bool isAutoEndRecord)
    {
        var endRecord = new AbsenceEndRecord
        {
            AbsenceRequestCtrlNbr = absenceRequestCtrlNbr,
            ActualEndUtc = actualEndUtc,
            IsAutoEndRecord = isAutoEndRecord
        };

        endRecord.Raise(new AbsenceEndedDomainEvent(endRecord.CtrlNbr, absenceRequestCtrlNbr));
        return endRecord;
    }
}

public sealed class VacancyImpact : Entity
{
    public ControlNumber AbsenceRequestCtrlNbr { get; private set; }
    public ControlNumber PositionSlotCtrlNbr { get; private set; }
    public DateTime ImpactStartUtc { get; private set; }
    public DateTime? ImpactEndUtc { get; private set; }

    private VacancyImpact() { AbsenceRequestCtrlNbr = null!; PositionSlotCtrlNbr = null!; }

    public static VacancyImpact Create(ControlNumber absenceRequestCtrlNbr, ControlNumber positionSlotCtrlNbr, DateTime impactStartUtc, DateTime? impactEndUtc = null)
    {
        var impact = new VacancyImpact
        {
            AbsenceRequestCtrlNbr = absenceRequestCtrlNbr,
            PositionSlotCtrlNbr = positionSlotCtrlNbr,
            ImpactStartUtc = impactStartUtc,
            ImpactEndUtc = impactEndUtc
        };
        impact.Raise(new VacancyImpactCreatedDomainEvent(impact));
        return impact;
    }

    public void ClearByMarkUp(DateTime markUpUtc)
    {
        ImpactEndUtc = markUpUtc;
    }
}

public static class AbsenceRequestWaitListType
{
    public const string CompensableDay = "COMPENSABLE_DAY";
    public const string VacationWeek = "VACATION_WEEK";
}

public sealed class AbsenceRequestWaitListRecord : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber AbsenceCodeCtrlNbr { get; private set; }
    public DateTime RequestDateUtc { get; private set; }
    public DateTime EntryUtc { get; private set; }
    public string WaitListType { get; private set; } = string.Empty;
    public ControlNumber? CraftCtrlNbr { get; private set; }
    public ControlNumber? DepartmentCtrlNbr { get; private set; }
    public DateTime? AssignedAtUtc { get; private set; }
    public string? AssignmentNotes { get; private set; }

    private AbsenceRequestWaitListRecord()
    {
        EmployeeCtrlNbr = null!;
        AbsenceCodeCtrlNbr = null!;
    }

    public static AbsenceRequestWaitListRecord CreateCompensableDay(
        ControlNumber employeeCtrlNbr,
        ControlNumber absenceCodeCtrlNbr,
        DateTime requestDateUtc,
        DateTime entryUtc,
        ControlNumber? craftCtrlNbr,
        ControlNumber? departmentCtrlNbr)
    {
        return CreateInternal(
            employeeCtrlNbr,
            absenceCodeCtrlNbr,
            requestDateUtc,
            entryUtc,
            AbsenceRequestWaitListType.CompensableDay,
            craftCtrlNbr,
            departmentCtrlNbr);
    }

    public static AbsenceRequestWaitListRecord CreateVacationWeek(
        ControlNumber employeeCtrlNbr,
        ControlNumber absenceCodeCtrlNbr,
        DateTime requestDateUtc,
        DateTime entryUtc,
        ControlNumber? craftCtrlNbr,
        ControlNumber? departmentCtrlNbr)
    {
        return CreateInternal(
            employeeCtrlNbr,
            absenceCodeCtrlNbr,
            requestDateUtc,
            entryUtc,
            AbsenceRequestWaitListType.VacationWeek,
            craftCtrlNbr,
            departmentCtrlNbr);
    }

    public void MarkAssigned(DateTime assignedAtUtc, string? assignmentNotes = null)
    {
        if (AssignedAtUtc.HasValue)
            throw new InvalidOperationException("Waitlist record is already assigned.");

        AssignedAtUtc = AsUtc(assignedAtUtc);
        AssignmentNotes = string.IsNullOrWhiteSpace(assignmentNotes)
            ? null
            : assignmentNotes.Trim();
    }

    private static AbsenceRequestWaitListRecord CreateInternal(
        ControlNumber employeeCtrlNbr,
        ControlNumber absenceCodeCtrlNbr,
        DateTime requestDateUtc,
        DateTime entryUtc,
        string waitListType,
        ControlNumber? craftCtrlNbr,
        ControlNumber? departmentCtrlNbr)
    {
        if (string.IsNullOrWhiteSpace(waitListType))
            throw new InvalidOperationException("Waitlist type is required.");

        var requestDate = AsUtc(requestDateUtc).Date;
        var entry = AsUtc(entryUtc);

        if (entry < requestDate)
            entry = requestDate;

        return new AbsenceRequestWaitListRecord
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            AbsenceCodeCtrlNbr = absenceCodeCtrlNbr,
            RequestDateUtc = requestDate,
            EntryUtc = entry,
            WaitListType = waitListType,
            CraftCtrlNbr = craftCtrlNbr,
            DepartmentCtrlNbr = departmentCtrlNbr
        };
    }

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}

public sealed class AbsenceRequestWaitListLink : Entity
{
    public ControlNumber AbsenceRequestCtrlNbr { get; private set; }
    public ControlNumber AbsenceRequestWaitListRecordCtrlNbr { get; private set; }

    private AbsenceRequestWaitListLink()
    {
        AbsenceRequestCtrlNbr = null!;
        AbsenceRequestWaitListRecordCtrlNbr = null!;
    }

    public static AbsenceRequestWaitListLink Create(
        ControlNumber absenceRequestCtrlNbr,
        ControlNumber absenceRequestWaitListRecordCtrlNbr)
    {
        return new AbsenceRequestWaitListLink
        {
            AbsenceRequestCtrlNbr = absenceRequestCtrlNbr,
            AbsenceRequestWaitListRecordCtrlNbr = absenceRequestWaitListRecordCtrlNbr
        };
    }
}

// Domain Events
public sealed record AbsenceRequestedDomainEvent : DomainEvent
{
    public AbsenceRequestedDomainEvent(AbsenceRequest r) : base(nameof(AbsenceRequest), r.CtrlNbr.Value, new { EmployeeCtrlNbr = r.EmployeeCtrlNbr.Value, r.ReasonCode }) { }
}
public sealed record AbsenceApprovedDomainEvent : DomainEvent
{
    public AbsenceApprovedDomainEvent(AbsenceRequest r) : base(nameof(AbsenceRequest), r.CtrlNbr.Value, new { EmployeeCtrlNbr = r.EmployeeCtrlNbr.Value }) { }
}
public sealed record AbsenceCompletedByMarkUpDomainEvent : DomainEvent
{
    public AbsenceCompletedByMarkUpDomainEvent(AbsenceRequest r) : base(nameof(AbsenceRequest), r.CtrlNbr.Value, new { EmployeeCtrlNbr = r.EmployeeCtrlNbr.Value }) { }
}
public sealed record VacancyImpactCreatedDomainEvent : DomainEvent
{
    public VacancyImpactCreatedDomainEvent(VacancyImpact v) : base(nameof(VacancyImpact), v.CtrlNbr.Value, new { PositionSlotCtrlNbr = v.PositionSlotCtrlNbr.Value }) { }
}
