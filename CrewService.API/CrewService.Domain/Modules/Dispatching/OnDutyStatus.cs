namespace CrewService.Domain.Modules.Dispatching;

/// <summary>
/// Lifecycle state of an <see cref="OnDutyRecord"/>. Implemented as a smart-enum value object so the
/// allowed states are strongly typed and discoverable, while persisting as the original string value
/// (no schema change). Transitions: <see cref="Scheduled"/> (planned at call-sheet generation) →
/// <see cref="OnDuty"/> (employee actually on duty) → <see cref="TiedUp"/> (tour complete).
/// <see cref="Called"/> is used when a record is created via manual placement.
/// </summary>
public sealed record OnDutyStatus
{
    /// <summary>Planned incumbent record created at call-sheet generation; not yet on duty.</summary>
    public static readonly OnDutyStatus Scheduled = new("Scheduled");

    /// <summary>Record created via manual placement before the employee is marked on duty.</summary>
    public static readonly OnDutyStatus Called = new("Called");

    /// <summary>Employee is currently on duty for the tour.</summary>
    public static readonly OnDutyStatus OnDuty = new("OnDuty");

    /// <summary>Tour is complete; the employee has tied up.</summary>
    public static readonly OnDutyStatus TiedUp = new("TiedUp");

    /// <summary>All defined statuses, used for lookups and rehydration.</summary>
    public static readonly IReadOnlyList<OnDutyStatus> All = [Scheduled, Called, OnDuty, TiedUp];

    /// <summary>The persisted string representation of the status.</summary>
    public string Value { get; }

    private OnDutyStatus(string value) => Value = value;

    /// <summary>
    /// Rehydrates an <see cref="OnDutyStatus"/> from its persisted string value.
    /// Throws when the value does not match a known status.
    /// </summary>
    public static OnDutyStatus FromValue(string? value) =>
        All.FirstOrDefault(s => s.Value == value)
        ?? throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown {nameof(OnDutyStatus)} value.");

    public override string ToString() => Value;
}
