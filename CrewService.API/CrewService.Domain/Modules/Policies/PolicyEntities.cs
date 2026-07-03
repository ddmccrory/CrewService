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

public static class SeniorityMoveType
{
    public const string Voluntary = "Voluntary";
    public const string ForceAssign = "ForceAssign";
    /// <summary>
    /// Administrative forced direct bump ("No Access"). Bypasses normal eligibility and
    /// effective-date strategy, takes effect on the next day, cancels the moving employee's
    /// own pending voluntary moves at execution, and co-assigns any open bulletin on the
    /// claimed crew position. Mirrors SA's <c>SeniorityMove</c> with <c>MoveType == "NA"</c>.
    /// </summary>
    public const string NoAccess = "NoAccess";
}

public static class SeniorityMoveStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
}

/// <summary>
/// Effective-date strategy values stored per transition path on <see cref="SeniorityMovePolicy"/>.
/// An empty string means that transition is not configured and moves of that type are blocked.
/// </summary>
public static class SeniorityMoveEffectiveDateStrategy
{
    /// <summary>Effective immediately (e.g. hangout board to crew).</summary>
    public const string Immediate = "Immediate";
    /// <summary>
    /// Effective at the off-duty (end-of-shift) time on the last working day of the relevant
    /// schedule period. Rolls +7 days when within the RequestHours lead-time window. For Engineers
    /// moving to board: uses current crew schedule (end of work week). For Crew-to-Crew or
    /// ExtraBoard-to-Crew: uses the TARGET position's schedule.
    /// </summary>
    public const string FirstOffDay = "FirstOffDay";
    /// <summary>
    /// Effective at now + RequestHours with a BumpDate floor. No schedule lookup.
    /// Matches legacy behavior for Trainman/Conductor/default crafts moving to a board.
    /// </summary>
    public const string RequestLeadTime = "RequestLeadTime";
}
public sealed class SeniorityMovePolicy : Entity
{
    public ControlNumber RailroadCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public int EligibilityDays { get; private set; }
    /// <summary>How many hours in advance a move request must be submitted (minimum lead time).
    /// Also used for early-submission: employee may request (RequestHours/24) days before fully qualifying.</summary>
    public int RequestHours { get; private set; }
    /// <summary>How many hours before the effective time a move can still be cancelled.</summary>
    public int CancelHours { get; private set; }
    /// <summary>
    /// When true the railroad/craft offers a "will work" choice to the moving employee
    /// when the move becomes effective at the start of a shift they would otherwise work
    /// (i.e. the effective time-of-day equals the current position's on-duty time).
    /// The employee's choice is captured on the <see cref="SeniorityMove"/>. Mirrors SA's
    /// per-move <c>SeniorityMoveWillWork</c> record, generalized as a tenant-level setting.
    /// </summary>
    public bool WillWorkEnabled { get; private set; }
    /// <summary>
    /// When true the <c>SeniorityMoveWorker</c> automatically approves Pending moves
    /// that satisfy the eligibility threshold. Mirrors SA's <c>AutoProcess</c> flag.
    /// </summary>
    public bool AutoApprove { get; private set; } = true;

    // -- Effective-date strategies per transition path --------------------------
    // Empty string = that transition is not configured (moves blocked).
    // Use SeniorityMoveEffectiveDateStrategy constants for valid values.

    /// <summary>Crew position -> Crew position bump.</summary>
    public string CrewToCrewStrategy { get; private set; } = string.Empty;
    /// <summary>Crew position -> Board (extra board or other AllowSeniorityMove board).</summary>
    public string CrewToBoardStrategy { get; private set; } = string.Empty;
    /// <summary>Extra board -> Crew position bump.</summary>
    public string ExtraBoardToCrewStrategy { get; private set; } = string.Empty;
    /// <summary>Hangout board -> Crew position bump.</summary>
    public string HangoutToCrewStrategy { get; private set; } = string.Empty;
    /// <summary>Extended absence board -> Crew position bump.</summary>
    public string ExtendedAbsenceToCrewStrategy { get; private set; } = string.Empty;
    /// <summary>Training board -> Crew position bump.</summary>
    public string TrainingToCrewStrategy { get; private set; } = string.Empty;
    /// <summary>New hire board -> Crew position bump.</summary>
    public string NewHireToCrewStrategy { get; private set; } = string.Empty;

    private SeniorityMovePolicy() { RailroadCtrlNbr = null!; CraftCtrlNbr = null!; }

    public static SeniorityMovePolicy Create(ControlNumber railroadCtrlNbr, ControlNumber craftCtrlNbr,
        int eligibilityDays, int requestHours = 0, int cancelHours = 0, bool autoApprove = true,
        string crewToCrewStrategy = "", string crewToBoardStrategy = "",
        string extraBoardToCrewStrategy = "", string hangoutToCrewStrategy = "",
        string extendedAbsenceToCrewStrategy = "", string trainingToCrewStrategy = "",
        string newHireToCrewStrategy = "", bool willWorkEnabled = false)
    {
        return new SeniorityMovePolicy
        {
            RailroadCtrlNbr = railroadCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            EligibilityDays = eligibilityDays,
            RequestHours = requestHours,
            CancelHours = cancelHours,
            AutoApprove = autoApprove,
            CrewToCrewStrategy = crewToCrewStrategy,
            CrewToBoardStrategy = crewToBoardStrategy,
            ExtraBoardToCrewStrategy = extraBoardToCrewStrategy,
            HangoutToCrewStrategy = hangoutToCrewStrategy,
            ExtendedAbsenceToCrewStrategy = extendedAbsenceToCrewStrategy,
            TrainingToCrewStrategy = trainingToCrewStrategy,
            NewHireToCrewStrategy = newHireToCrewStrategy,
            WillWorkEnabled = willWorkEnabled
        };
    }

    public void Update(int eligibilityDays, int requestHours, int cancelHours, bool autoApprove,
        string crewToCrewStrategy, string crewToBoardStrategy,
        string extraBoardToCrewStrategy, string hangoutToCrewStrategy,
        string extendedAbsenceToCrewStrategy, string trainingToCrewStrategy,
        string newHireToCrewStrategy, bool willWorkEnabled = false)
    {
        EligibilityDays = eligibilityDays;
        RequestHours = requestHours;
        CancelHours = cancelHours;
        AutoApprove = autoApprove;
        CrewToCrewStrategy = crewToCrewStrategy;
        CrewToBoardStrategy = crewToBoardStrategy;
        ExtraBoardToCrewStrategy = extraBoardToCrewStrategy;
        HangoutToCrewStrategy = hangoutToCrewStrategy;
        ExtendedAbsenceToCrewStrategy = extendedAbsenceToCrewStrategy;
        TrainingToCrewStrategy = trainingToCrewStrategy;
        NewHireToCrewStrategy = newHireToCrewStrategy;
        WillWorkEnabled = willWorkEnabled;
    }
}

public sealed class SeniorityMove : Entity
{
    public ControlNumber RailroadCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public ControlNumber TargetPositionCtrlNbr { get; private set; }
    public ControlNumber? DisplacedEmployeeCtrlNbr { get; private set; }
    public DateTime RequestedUtc { get; private set; }
    public DateTime? EffectiveUtc { get; private set; }
    public int DaysOnCurrentPosition { get; private set; }
    public string MoveType { get; private set; } = SeniorityMoveType.Voluntary;
    public string Status { get; private set; } = SeniorityMoveStatus.Pending;
    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// The employee's "will work" election for the final shift that overlaps the move's
    /// effective time, captured only when the governing policy has <see cref="SeniorityMovePolicy.WillWorkEnabled"/>
    /// and the move qualifies (effective time-of-day equals the current position's on-duty time).
    /// <c>null</c> means no election was offered/recorded. Mirrors SA's <c>SeniorityMoveWillWork</c> record.
    /// </summary>
    public bool? WillWork { get; private set; }

    private SeniorityMove() { RailroadCtrlNbr = null!; EmployeeCtrlNbr = null!; CraftCtrlNbr = null!; TargetPositionCtrlNbr = null!; }

    public static SeniorityMove Create(ControlNumber railroadCtrlNbr, ControlNumber employeeCtrlNbr, ControlNumber craftCtrlNbr,
        ControlNumber targetPositionCtrlNbr, ControlNumber? displacedEmployeeCtrlNbr,
        int daysOnCurrentPosition, string moveType = SeniorityMoveType.Voluntary,
        DateTime? effectiveUtc = null, bool? willWork = null)
    {
        var move = new SeniorityMove
        {
            RailroadCtrlNbr = railroadCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            TargetPositionCtrlNbr = targetPositionCtrlNbr,
            DisplacedEmployeeCtrlNbr = displacedEmployeeCtrlNbr,
            RequestedUtc = DateTime.UtcNow,
            EffectiveUtc = effectiveUtc,
            DaysOnCurrentPosition = daysOnCurrentPosition,
            MoveType = moveType,
            Status = SeniorityMoveStatus.Pending,
            WillWork = willWork
        };
        move.Raise(new SeniorityMoveRequestedDomainEvent(employeeCtrlNbr, targetPositionCtrlNbr, craftCtrlNbr, moveType));
        return move;
    }

    /// <summary>
    /// Records the employee's "will work" election for the final overlapping shift.
    /// Only valid while the move is still Pending (before it is approved/executed).
    /// </summary>
    public void SetWillWork(bool willWork)
    {
        if (Status != SeniorityMoveStatus.Pending)
            throw new InvalidOperationException($"Cannot change the will-work election on a seniority move in status '{Status}'.");
        WillWork = willWork;
    }

    public void Approve(DateTime? effectiveUtc = null)
    {
        if (Status != SeniorityMoveStatus.Pending)
            throw new InvalidOperationException($"Cannot approve a seniority move in status '{Status}'.");
        Status = SeniorityMoveStatus.Approved;
        if (effectiveUtc.HasValue) EffectiveUtc = effectiveUtc;
        Raise(new SeniorityMoveApprovedDomainEvent(EmployeeCtrlNbr, TargetPositionCtrlNbr, CraftCtrlNbr));
    }

    public void Reject(string reason)
    {
        if (Status != SeniorityMoveStatus.Pending)
            throw new InvalidOperationException($"Cannot reject a seniority move in status '{Status}'.");
        Status = SeniorityMoveStatus.Rejected;
        RejectionReason = reason;
        Raise(new SeniorityMoveRejectedDomainEvent(EmployeeCtrlNbr, CraftCtrlNbr, reason));
    }

    public void Cancel(string reason)
    {
        if (Status == SeniorityMoveStatus.Completed || Status == SeniorityMoveStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a seniority move in status '{Status}'.");
        Status = SeniorityMoveStatus.Cancelled;
        CancellationReason = reason;
        Raise(new SeniorityMoveCancelledDomainEvent(EmployeeCtrlNbr, CraftCtrlNbr, reason));
    }

    public void Complete()
    {
        if (Status != SeniorityMoveStatus.Approved)
            throw new InvalidOperationException($"Cannot complete a seniority move in status '{Status}'.");
        Status = SeniorityMoveStatus.Completed;
        EffectiveUtc ??= DateTime.UtcNow;
        Raise(new SeniorityMoveCompletedDomainEvent(EmployeeCtrlNbr, TargetPositionCtrlNbr, CraftCtrlNbr));
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

public sealed record SeniorityMoveRequestedDomainEvent : DomainEvent
{
    public SeniorityMoveRequestedDomainEvent(ControlNumber employeeCtrlNbr, ControlNumber targetPositionCtrlNbr, ControlNumber craftCtrlNbr, string moveType)
        : base("SeniorityMove", employeeCtrlNbr.Value, new { TargetPositionCtrlNbr = targetPositionCtrlNbr.Value, CraftCtrlNbr = craftCtrlNbr.Value, MoveType = moveType }) { }
}

public sealed record SeniorityMoveApprovedDomainEvent : DomainEvent
{
    public SeniorityMoveApprovedDomainEvent(ControlNumber employeeCtrlNbr, ControlNumber targetPositionCtrlNbr, ControlNumber craftCtrlNbr)
        : base("SeniorityMove", employeeCtrlNbr.Value, new { TargetPositionCtrlNbr = targetPositionCtrlNbr.Value, CraftCtrlNbr = craftCtrlNbr.Value }) { }
}

public sealed record SeniorityMoveRejectedDomainEvent : DomainEvent
{
    public SeniorityMoveRejectedDomainEvent(ControlNumber employeeCtrlNbr, ControlNumber craftCtrlNbr, string reason)
        : base("SeniorityMove", employeeCtrlNbr.Value, new { CraftCtrlNbr = craftCtrlNbr.Value, Reason = reason }) { }
}

public sealed record SeniorityMoveCancelledDomainEvent : DomainEvent
{
    public SeniorityMoveCancelledDomainEvent(ControlNumber employeeCtrlNbr, ControlNumber craftCtrlNbr, string reason)
        : base("SeniorityMove", employeeCtrlNbr.Value, new { CraftCtrlNbr = craftCtrlNbr.Value, Reason = reason }) { }
}

public sealed record SeniorityMoveCompletedDomainEvent : DomainEvent
{
    public SeniorityMoveCompletedDomainEvent(ControlNumber employeeCtrlNbr, ControlNumber targetPositionCtrlNbr, ControlNumber craftCtrlNbr)
        : base("SeniorityMove", employeeCtrlNbr.Value, new { TargetPositionCtrlNbr = targetPositionCtrlNbr.Value, CraftCtrlNbr = craftCtrlNbr.Value }) { }
}
