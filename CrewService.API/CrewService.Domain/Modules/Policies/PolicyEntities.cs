using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Policies;

public sealed class CraftDisplacementPolicy : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }
    public int WindowHours { get; private set; }
    public string SeniorityBasis { get; private set; } = string.Empty;
    public string DefaultAction { get; private set; } = string.Empty;
    public string? EligibilitySelectorJson { get; private set; }

    private CraftDisplacementPolicy() { CraftCtrlNbr = null!; }

    public static CraftDisplacementPolicy Create(long craftCtrlNbr, int windowHours, string seniorityBasis, string defaultAction, string? eligibilitySelectorJson = null)
    {
        return new CraftDisplacementPolicy
        {
            CraftCtrlNbr = ControlNumber.Create(craftCtrlNbr),
            WindowHours = windowHours,
            SeniorityBasis = seniorityBasis,
            DefaultAction = defaultAction,
            EligibilitySelectorJson = eligibilitySelectorJson
        };
    }

    public void Update(int windowHours, string seniorityBasis, string defaultAction, string? eligibilitySelectorJson)
    {
        WindowHours = windowHours;
        SeniorityBasis = seniorityBasis;
        DefaultAction = defaultAction;
        EligibilitySelectorJson = eligibilitySelectorJson;
    }
}

public sealed class DisplacementCase : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public DateTime OpenedUtc { get; private set; }
    public DateTime ExpiresUtc { get; private set; }
    public string Status { get; private set; } = "Open";

    private DisplacementCase() { EmployeeCtrlNbr = null!; CraftCtrlNbr = null!; }

    public static DisplacementCase Create(long employeeCtrlNbr, long craftCtrlNbr, DateTime openedUtc, DateTime expiresUtc)
    {
        var dc = new DisplacementCase
        {
            EmployeeCtrlNbr = ControlNumber.Create(employeeCtrlNbr),
            CraftCtrlNbr = ControlNumber.Create(craftCtrlNbr),
            OpenedUtc = openedUtc,
            ExpiresUtc = expiresUtc
        };
        dc.Raise(new DisplacementCaseOpenedDomainEvent(dc));
        return dc;
    }

    public void Close(string status)
    {
        Status = status;
    }
}

public sealed class DisplacementClaim : Entity
{
    public ControlNumber CaseCtrlNbr { get; private set; }
    public ControlNumber TargetEmployeeCtrlNbr { get; private set; }
    public DateTime SubmittedUtc { get; private set; }
    public string? Decision { get; private set; }
    public DateTime? DecidedUtc { get; private set; }
    public string? Reason { get; private set; }

    private DisplacementClaim() { CaseCtrlNbr = null!; TargetEmployeeCtrlNbr = null!; }

    public static DisplacementClaim Create(long caseCtrlNbr, long targetEmployeeCtrlNbr, DateTime submittedUtc)
    {
        return new DisplacementClaim
        {
            CaseCtrlNbr = ControlNumber.Create(caseCtrlNbr),
            TargetEmployeeCtrlNbr = ControlNumber.Create(targetEmployeeCtrlNbr),
            SubmittedUtc = submittedUtc
        };
    }

    public void Decide(string decision, string? reason)
    {
        Decision = decision;
        DecidedUtc = DateTime.UtcNow;
        Reason = reason;
    }
}

// Domain Events
public sealed record DisplacementCaseOpenedDomainEvent : DomainEvent
{
    public DisplacementCaseOpenedDomainEvent(DisplacementCase c)
        : base(nameof(DisplacementCase), c.CtrlNbr.Value, new { EmployeeCtrlNbr = c.EmployeeCtrlNbr.Value, CraftCtrlNbr = c.CraftCtrlNbr.Value }) { }
}
