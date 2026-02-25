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

public sealed class BulletinPolicy : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }
    public int BidWindowHours { get; private set; }
    public bool ForcedAssignmentEnabled { get; private set; }
    public string ForcedAssignmentBasis { get; private set; } = "JUNIOR_FIRST";

    private BulletinPolicy() { CraftCtrlNbr = null!; }

    public static BulletinPolicy Create(long craftCtrlNbr, int bidWindowHours,
        bool forcedAssignmentEnabled = true, string forcedAssignmentBasis = "JUNIOR_FIRST")
    {
        return new BulletinPolicy
        {
            CraftCtrlNbr = ControlNumber.Create(craftCtrlNbr),
            BidWindowHours = bidWindowHours,
            ForcedAssignmentEnabled = forcedAssignmentEnabled,
            ForcedAssignmentBasis = forcedAssignmentBasis
        };
    }

    public void Update(int bidWindowHours, bool forcedAssignmentEnabled, string forcedAssignmentBasis)
    {
        BidWindowHours = bidWindowHours;
        ForcedAssignmentEnabled = forcedAssignmentEnabled;
        ForcedAssignmentBasis = forcedAssignmentBasis;
    }
}

public sealed class SeniorityMovePolicy : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }
    public int EligibilityDays { get; private set; }
    public string SeniorityBasis { get; private set; } = string.Empty;

    private SeniorityMovePolicy() { CraftCtrlNbr = null!; }

    public static SeniorityMovePolicy Create(long craftCtrlNbr, int eligibilityDays, string seniorityBasis)
    {
        return new SeniorityMovePolicy
        {
            CraftCtrlNbr = ControlNumber.Create(craftCtrlNbr),
            EligibilityDays = eligibilityDays,
            SeniorityBasis = seniorityBasis
        };
    }

    public void Update(int eligibilityDays, string seniorityBasis)
    {
        EligibilityDays = eligibilityDays;
        SeniorityBasis = seniorityBasis;
    }
}

// Domain Events
public sealed record DisplacementCaseOpenedDomainEvent : DomainEvent
{
    public DisplacementCaseOpenedDomainEvent(DisplacementCase c)
        : base(nameof(DisplacementCase), c.CtrlNbr.Value, new { EmployeeCtrlNbr = c.EmployeeCtrlNbr.Value, CraftCtrlNbr = c.CraftCtrlNbr.Value }) { }
}

public sealed record DisplacementAutoPlacedDomainEvent : DomainEvent
{
    public DisplacementAutoPlacedDomainEvent(DisplacementCase c)
        : base(nameof(DisplacementCase), c.CtrlNbr.Value, new { EmployeeCtrlNbr = c.EmployeeCtrlNbr.Value, CraftCtrlNbr = c.CraftCtrlNbr.Value }) { }
}

public sealed record SeniorityMoveExercisedDomainEvent : DomainEvent
{
    public SeniorityMoveExercisedDomainEvent(long employeeCtrlNbr, long targetPositionCtrlNbr, long craftCtrlNbr)
        : base("SeniorityMove", employeeCtrlNbr, new { TargetPositionCtrlNbr = targetPositionCtrlNbr, CraftCtrlNbr = craftCtrlNbr }) { }
}
