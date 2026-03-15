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

    public static CraftDisplacementPolicy Create(ControlNumber craftCtrlNbr, int windowHours, string seniorityBasis, string defaultAction, string? eligibilitySelectorJson = null)
    {
        return new CraftDisplacementPolicy
        {
            CraftCtrlNbr = craftCtrlNbr,
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

    public static DisplacementCase Create(ControlNumber employeeCtrlNbr, ControlNumber craftCtrlNbr, DateTime openedUtc, DateTime expiresUtc)
    {
        var dc = new DisplacementCase
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
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

    public void AutoPlaceOnExtraBoard()
    {
        Status = "AutoPlaced";
        Raise(new DisplacementAutoPlacedDomainEvent(this));
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

    public static DisplacementClaim Create(ControlNumber caseCtrlNbr, ControlNumber targetEmployeeCtrlNbr, DateTime submittedUtc)
    {
        return new DisplacementClaim
        {
            CaseCtrlNbr = caseCtrlNbr,
            TargetEmployeeCtrlNbr = targetEmployeeCtrlNbr,
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

    public static BulletinPolicy Create(ControlNumber craftCtrlNbr, int bidWindowHours,
        bool forcedAssignmentEnabled = true, string forcedAssignmentBasis = "JUNIOR_FIRST")
    {
        return new BulletinPolicy
        {
            CraftCtrlNbr = craftCtrlNbr,
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

    public static SeniorityMovePolicy Create(ControlNumber craftCtrlNbr, int eligibilityDays, string seniorityBasis)
    {
        return new SeniorityMovePolicy
        {
            CraftCtrlNbr = craftCtrlNbr,
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

public sealed class SeniorityMove : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public ControlNumber TargetPositionCtrlNbr { get; private set; }
    public ControlNumber? DisplacedEmployeeCtrlNbr { get; private set; }
    public DateTime ExercisedUtc { get; private set; }
    public int DaysOnCurrentPosition { get; private set; }

    private SeniorityMove() { EmployeeCtrlNbr = null!; CraftCtrlNbr = null!; TargetPositionCtrlNbr = null!; }

    public static SeniorityMove Create(ControlNumber employeeCtrlNbr, ControlNumber craftCtrlNbr,
        ControlNumber targetPositionCtrlNbr, ControlNumber? displacedEmployeeCtrlNbr, int daysOnCurrentPosition)
    {
        var move = new SeniorityMove
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            TargetPositionCtrlNbr = targetPositionCtrlNbr,
            DisplacedEmployeeCtrlNbr = displacedEmployeeCtrlNbr,
            ExercisedUtc = DateTime.UtcNow,
            DaysOnCurrentPosition = daysOnCurrentPosition
        };
        move.Raise(new SeniorityMoveExercisedDomainEvent(
            employeeCtrlNbr, targetPositionCtrlNbr, craftCtrlNbr));
        return move;
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
    public SeniorityMoveExercisedDomainEvent(ControlNumber employeeCtrlNbr, ControlNumber targetPositionCtrlNbr, ControlNumber craftCtrlNbr)
        : base("SeniorityMove", employeeCtrlNbr.Value, new { TargetPositionCtrlNbr = targetPositionCtrlNbr.Value, CraftCtrlNbr = craftCtrlNbr.Value }) { }
}
