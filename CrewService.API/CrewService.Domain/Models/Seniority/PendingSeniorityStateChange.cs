using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Seniority;

public enum PendingStateChangeStatus
{
    Pending = 0,
    Applied = 1,
    Cancelled = 2
}

/// <summary>
/// A scheduled seniority state change that will be applied at <see cref="EffectiveDateUtc"/>.
/// Only one Pending record may exist per employee at a time.
/// </summary>
public sealed class PendingSeniorityStateChange : Entity
{
    public ControlNumber SeniorityCtrlNbr { get; private set; } = null!;
    public ControlNumber EmployeeCtrlNbr { get; private set; } = null!;

    /// <summary>Snapshot of the state at scheduling time — for audit.</summary>
    public ControlNumber FromSeniorityStateCtrlNbr { get; private set; } = null!;

    public ControlNumber ToSeniorityStateCtrlNbr { get; private set; } = null!;

    /// <summary>UTC time at which the state change should be applied.</summary>
    public DateTime EffectiveDateUtc { get; private set; }

    public PendingStateChangeStatus Status { get; private set; }

    public string ScheduledByUserId { get; private set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public string? CancelledByUserId { get; private set; }

    private PendingSeniorityStateChange() { }

    public static PendingSeniorityStateChange Schedule(
        ControlNumber seniorityCtrlNbr,
        ControlNumber employeeCtrlNbr,
        ControlNumber fromSeniorityStateCtrlNbr,
        ControlNumber toSeniorityStateCtrlNbr,
        DateTime effectiveDateUtc,
        string scheduledByUserId)
    {
        if (effectiveDateUtc <= DateTime.UtcNow)
            throw new ArgumentException(
                "EffectiveDateUtc must be in the future. Use UpdateAsync directly for immediate changes.",
                nameof(effectiveDateUtc));

        return new PendingSeniorityStateChange
        {
            SeniorityCtrlNbr = seniorityCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            FromSeniorityStateCtrlNbr = fromSeniorityStateCtrlNbr,
            ToSeniorityStateCtrlNbr = toSeniorityStateCtrlNbr,
            EffectiveDateUtc = effectiveDateUtc,
            Status = PendingStateChangeStatus.Pending,
            ScheduledByUserId = scheduledByUserId,
            ScheduledAtUtc = DateTime.UtcNow
        };
    }

    public void MarkApplied()
    {
        if (Status != PendingStateChangeStatus.Pending)
            throw new InvalidOperationException($"Cannot apply a change that is already {Status}.");

        Status = PendingStateChangeStatus.Applied;
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string cancelledByUserId)
    {
        if (Status != PendingStateChangeStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel a change that is already {Status}.");

        Status = PendingStateChangeStatus.Cancelled;
        ProcessedAtUtc = DateTime.UtcNow;
        CancelledByUserId = cancelledByUserId;
    }
}
