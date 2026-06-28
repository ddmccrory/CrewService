using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Employees;

/// <summary>Seniority row enriched with roster name, state description, and current position.</summary>
public sealed record WorkProfileSeniorityEntry(
    ControlNumber CtrlNbr,
    ControlNumber RosterCtrlNbr,
    string        RosterName,
    string        RosterDate,
    int           Rank,
    ControlNumber SeniorityStateCtrlNbr,
    string        SeniorityStateName,
    bool          LastActiveRoster,
    string        PositionName,
    string        PositionType,
    string        PositionAssignedDate,
    ControlNumber CraftCtrlNbr);

/// <summary>Bulletin bid enriched with bulletin code and position name.</summary>
public sealed record WorkProfileBulletinBid(
    ControlNumber CtrlNbr,
    ControlNumber BulletinCtrlNbr,
    int           Priority,
    DateTime      SubmittedUtc,
    string        Status,
    string        BulletinCode,
    string        PositionName);

/// <summary>Seniority move projected for the work-profile panel, enriched with
/// the resolved <paramref name="TargetPositionName"/> and the server-computed
/// <paramref name="CanCancel"/> action flag.</summary>
public sealed record WorkProfileSeniorityMoveItem(
    ControlNumber  CtrlNbr,
    ControlNumber  CraftCtrlNbr,
    ControlNumber  TargetPositionCtrlNbr,
    ControlNumber? DisplacedEmployeeCtrlNbr,
    DateTime       RequestedUtc,
    DateTime?      EffectiveUtc,
    int            DaysOnCurrentPosition,
    string         MoveType,
    string         Status,
    string?        RejectionReason,
    string?        CancellationReason,
    bool           CanCancel,
    string         TargetPositionName);

/// <summary>All data needed by the EmployeeDetail work-profile panel.</summary>
public sealed record EmployeeWorkProfileResult(
    string                             Role,
    string                             EmploymentDate,
    string                             EmploymentStatus,
    bool                               CanBidOnBulletins,
    IReadOnlyList<WorkProfileSeniorityEntry> SeniorityEntries,
    IReadOnlyList<WorkProfileSeniorityMoveItem> Moves,
    IReadOnlyList<WorkProfileBulletinBid>    Bids);
