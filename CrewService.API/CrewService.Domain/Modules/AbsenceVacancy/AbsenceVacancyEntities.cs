using CrewService.Domain.DomainEvents;
using CrewService.Domain.DomainEvents.Absence;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.AbsenceVacancy;

public sealed class AbsenceRequest : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public DateTime ScheduledStartUtc { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public string Status { get; private set; } = "PENDING";
    public ControlNumber? ApprovedByCtrlNbr { get; private set; }
    public string? Notes { get; private set; }
    public ControlNumber? AbsenceCodeCtrlNbr { get; private set; }
    public DateTime? MarkOffStartUtc { get; private set; }
    public bool IsSystemGenerated { get; private set; }
    public bool AutoMarkOffOnApproval { get; private set; }

    private readonly List<AbsenceApproval> _approvals = [];
    private readonly List<AbsenceMarkUp> _markUps = [];
    public IReadOnlyList<AbsenceApproval> Approvals => _approvals.AsReadOnly();
    public IReadOnlyList<AbsenceMarkUp> MarkUps => _markUps.AsReadOnly();

    private AbsenceRequest() { EmployeeCtrlNbr = null!; }

    public static AbsenceRequest Create(ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc, string reasonCode, string? notes = null)
    {
        var request = new AbsenceRequest
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            ScheduledStartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
            ReasonCode = reasonCode,
            Notes = notes
        };
        if (endUtc.HasValue)
            request.AddMarkUp(DateTime.SpecifyKind(endUtc.Value, DateTimeKind.Utc), isAutoMarkUp: true);
        request.Raise(new AbsenceRequestedDomainEvent(request));
        return request;
    }

    public static AbsenceRequest CreateWithCode(
        ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc,
        ControlNumber absenceCodeCtrlNbr, string reasonCode,
        bool isSystemGenerated = false, string? notes = null,
        bool autoMarkOffOnApproval = false)
    {
        var request = new AbsenceRequest
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            ScheduledStartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
            ReasonCode = reasonCode,
            AbsenceCodeCtrlNbr = absenceCodeCtrlNbr,
            IsSystemGenerated = isSystemGenerated,
            AutoMarkOffOnApproval = autoMarkOffOnApproval,
            Notes = notes
        };
        if (endUtc.HasValue)
            request.AddMarkUp(DateTime.SpecifyKind(endUtc.Value, DateTimeKind.Utc), isAutoMarkUp: true);
        request.Raise(new AbsenceRequestedDomainEvent(request));
        return request;
    }

    public void Approve(ControlNumber approvedByCtrlNbr)
    {
        Status = "APPROVED";
        ApprovedByCtrlNbr = approvedByCtrlNbr;
        Raise(new AbsenceApprovedDomainEvent(this));
    }

    public void Deny(ControlNumber deniedByCtrlNbr)
    {
        Status = "DENIED";
        ApprovedByCtrlNbr = deniedByCtrlNbr;
    }

    public void Cancel()
    {
        Status = "CANCELLED";
    }

    public void CompleteByMarkUp(DateTime markUpUtc)
    {
        if (!string.Equals(Status, "EXERCISED", StringComparison.OrdinalIgnoreCase))
            Status = "EXERCISED";
        Raise(new AbsenceCompletedByMarkUpDomainEvent(this));
    }

    public void Exercise(DateTime exercisedUtc)
    {
        if (!string.Equals(Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only approved absence requests can be marked off.");

        var exercised = DateTime.SpecifyKind(exercisedUtc, DateTimeKind.Utc);
        MarkOffStartUtc = exercised < ScheduledStartUtc ? ScheduledStartUtc : exercised;
        Status = "EXERCISED";
    }

    public AbsenceApproval AddApproval(ControlNumber approvalOfficerCtrlNbr)
    {
        var approval = AbsenceApproval.Create(CtrlNbr, approvalOfficerCtrlNbr);
        _approvals.Add(approval);
        return approval;
    }

    public AbsenceMarkUp AddMarkUp(DateTime scheduledMarkUpUtc, bool isAutoMarkUp)
    {
        var markUp = AbsenceMarkUp.Create(CtrlNbr, scheduledMarkUpUtc, isAutoMarkUp);
        _markUps.Add(markUp);
        return markUp;
    }
}

public sealed class AbsenceApproval : Entity
{
    public ControlNumber AbsenceRequestCtrlNbr { get; private set; }
    public ControlNumber ApprovalOfficerCtrlNbr { get; private set; }
    public string Status { get; private set; } = "PENDING";
    public DateTime? DecidedAtUtc { get; private set; }
    public string? Notes { get; private set; }

    private AbsenceApproval()
    {
        AbsenceRequestCtrlNbr = null!;
        ApprovalOfficerCtrlNbr = null!;
    }

    internal static AbsenceApproval Create(ControlNumber absenceRequestCtrlNbr, ControlNumber approvalOfficerCtrlNbr)
    {
        return new AbsenceApproval
        {
            AbsenceRequestCtrlNbr = absenceRequestCtrlNbr,
            ApprovalOfficerCtrlNbr = approvalOfficerCtrlNbr
        };
    }

    public void Approve(string? notes = null)
    {
        Status = "APPROVED";
        DecidedAtUtc = DateTime.UtcNow;
        Notes = notes;
        Raise(new AbsenceApprovalDecidedDomainEvent(CtrlNbr, AbsenceRequestCtrlNbr, "APPROVED"));
    }

    public void Decline(string? notes = null)
    {
        Status = "DECLINED";
        DecidedAtUtc = DateTime.UtcNow;
        Notes = notes;
        Raise(new AbsenceApprovalDecidedDomainEvent(CtrlNbr, AbsenceRequestCtrlNbr, "DECLINED"));
    }
}

public sealed class AbsenceMarkUp : Entity
{
    public ControlNumber AbsenceRequestCtrlNbr { get; private set; }
    public DateTime ScheduledMarkUpUtc { get; private set; }
    public DateTime? ActualMarkUpUtc { get; private set; }
    public bool IsAutoMarkUp { get; private set; }

    private AbsenceMarkUp() { AbsenceRequestCtrlNbr = null!; }

    internal static AbsenceMarkUp Create(ControlNumber absenceRequestCtrlNbr, DateTime scheduledMarkUpUtc, bool isAutoMarkUp)
    {
        var markUp = new AbsenceMarkUp
        {
            AbsenceRequestCtrlNbr = absenceRequestCtrlNbr,
            ScheduledMarkUpUtc = scheduledMarkUpUtc,
            IsAutoMarkUp = isAutoMarkUp
        };
        markUp.Raise(new AbsenceMarkUpScheduledDomainEvent(markUp.CtrlNbr, absenceRequestCtrlNbr, scheduledMarkUpUtc));
        return markUp;
    }

    public void Execute(DateTime actualMarkUpUtc)
    {
        ActualMarkUpUtc = actualMarkUpUtc;
        Raise(new AbsenceMarkedUpDomainEvent(CtrlNbr, AbsenceRequestCtrlNbr));
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
