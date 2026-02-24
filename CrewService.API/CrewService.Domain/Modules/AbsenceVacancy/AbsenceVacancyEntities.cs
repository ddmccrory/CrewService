using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.AbsenceVacancy;

public sealed class AbsenceRequest : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public string Status { get; private set; } = "PENDING";
    public ControlNumber? ApprovedByCtrlNbr { get; private set; }
    public string? Notes { get; private set; }

    private AbsenceRequest() { EmployeeCtrlNbr = null!; }

    public static AbsenceRequest Create(long employeeCtrlNbr, DateTime startUtc, DateTime? endUtc, string reasonCode, string? notes = null)
    {
        var request = new AbsenceRequest
        {
            EmployeeCtrlNbr = ControlNumber.Create(employeeCtrlNbr),
            StartUtc = startUtc,
            EndUtc = endUtc,
            ReasonCode = reasonCode,
            Notes = notes
        };
        request.Raise(new AbsenceRequestedDomainEvent(request));
        return request;
    }

    public void Approve(long approvedByCtrlNbr)
    {
        Status = "APPROVED";
        ApprovedByCtrlNbr = ControlNumber.Create(approvedByCtrlNbr);
        Raise(new AbsenceApprovedDomainEvent(this));
    }

    public void Deny(long deniedByCtrlNbr)
    {
        Status = "DENIED";
        ApprovedByCtrlNbr = ControlNumber.Create(deniedByCtrlNbr);
    }

    public void Cancel()
    {
        Status = "CANCELLED";
    }

    public void CompleteByMarkUp(DateTime markUpUtc)
    {
        Status = "COMPLETED";
        EndUtc = markUpUtc;
        Raise(new AbsenceCompletedByMarkUpDomainEvent(this));
    }
}

public sealed class VacancyImpact : Entity
{
    public ControlNumber AbsenceRequestCtrlNbr { get; private set; }
    public ControlNumber PositionSlotCtrlNbr { get; private set; }
    public DateTime ImpactStartUtc { get; private set; }
    public DateTime? ImpactEndUtc { get; private set; }

    private VacancyImpact() { AbsenceRequestCtrlNbr = null!; PositionSlotCtrlNbr = null!; }

    public static VacancyImpact Create(long absenceRequestCtrlNbr, long positionSlotCtrlNbr, DateTime impactStartUtc, DateTime? impactEndUtc = null)
    {
        var impact = new VacancyImpact
        {
            AbsenceRequestCtrlNbr = ControlNumber.Create(absenceRequestCtrlNbr),
            PositionSlotCtrlNbr = ControlNumber.Create(positionSlotCtrlNbr),
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
