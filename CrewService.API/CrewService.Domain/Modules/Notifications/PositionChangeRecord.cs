using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Notifications;

/// <summary>
/// Operational read-model artifact that captures incumbent-affecting position-change semantics
/// (legacy RailroadPositionChange parity) independent of user-facing notification rendering.
/// </summary>
public sealed class PositionChangeRecord : Entity
{
    public ControlNumber RailroadCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber? EmployeeNotificationCtrlNbr { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public ControlNumber? SourceCtrlNbr { get; private set; }
    public string ChangeType { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTime? EffectiveAtUtc { get; private set; }
    public bool RequiresAcknowledgement { get; private set; }
    public bool IsOpen { get; private set; }
    public DateTime OpenedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public string? ClosedReason { get; private set; }

    private PositionChangeRecord()
    {
        RailroadCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static PositionChangeRecord Create(
        ControlNumber railroadCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string sourceType,
        ControlNumber? sourceCtrlNbr,
        string changeType,
        string message,
        bool requiresAcknowledgement,
        DateTime? effectiveAtUtc = null,
        ControlNumber? employeeNotificationCtrlNbr = null)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new ArgumentException("SourceType is required.", nameof(sourceType));
        if (string.IsNullOrWhiteSpace(changeType))
            throw new ArgumentException("ChangeType is required.", nameof(changeType));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        return new PositionChangeRecord
        {
            RailroadCtrlNbr = railroadCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            EmployeeNotificationCtrlNbr = employeeNotificationCtrlNbr,
            SourceType = sourceType.Trim(),
            SourceCtrlNbr = sourceCtrlNbr,
            ChangeType = changeType.Trim(),
            Message = message.Trim(),
            EffectiveAtUtc = effectiveAtUtc,
            RequiresAcknowledgement = requiresAcknowledgement,
            IsOpen = requiresAcknowledgement,
            OpenedAtUtc = DateTime.UtcNow,
            ClosedAtUtc = requiresAcknowledgement ? null : DateTime.UtcNow,
            ClosedReason = requiresAcknowledgement ? null : PositionChangeClosedReasons.Informational
        };
    }

    public void MarkAcknowledged(string acknowledgedByUser)
    {
        _ = acknowledgedByUser;
        if (!IsOpen) return;

        IsOpen = false;
        ClosedAtUtc = DateTime.UtcNow;
        ClosedReason = PositionChangeClosedReasons.Acknowledged;
    }

    public void MarkSuperseded(string reason)
    {
        if (!IsOpen) return;

        IsOpen = false;
        ClosedAtUtc = DateTime.UtcNow;
        ClosedReason = string.IsNullOrWhiteSpace(reason)
            ? PositionChangeClosedReasons.Superseded
            : reason.Trim();
    }
}

public static class PositionChangeSourceTypes
{
    public const string Notification = "Notification";
    public const string SeniorityMove = "SeniorityMove";
    public const string Bulletin = "Bulletin";
    public const string RosterBoard = "RosterBoard";
}

public static class PositionChangeTypes
{
    public const string BumpRequested = "BumpRequested";
    public const string BumpCancelled = "BumpCancelled";
    public const string MoveExecuted = "MoveExecuted";
    public const string BulletinAwarded = "BulletinAwarded";
    public const string ForcedAssignment = "ForcedAssignment";
    public const string BoardPlacement = "BoardPlacement";
    public const string Informational = "Informational";
}

public static class PositionChangeClosedReasons
{
    public const string Acknowledged = "Acknowledged";
    public const string Superseded = "Superseded";
    public const string Informational = "Informational";
    public const string Cancelled = "Cancelled";
}