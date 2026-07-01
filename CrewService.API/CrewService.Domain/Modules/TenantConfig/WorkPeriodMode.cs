namespace CrewService.Domain.Modules.TenantConfig;

/// <summary>
/// Defines how a railroad's "work period" is calculated for on-duty history filtering
/// (the legacy "Current Work Period" / "Previous Work Period" pay-period slices).
/// Implemented as a smart-enum value object so the allowed modes are strongly typed and
/// discoverable, while persisting as the original string value (no schema conversion needed).
/// The default <see cref="HalfMonth"/> preserves legacy behavior (1st–15th and 16th–end-of-month).
/// </summary>
public sealed record WorkPeriodMode
{
    /// <summary>Legacy behavior: two periods per month — the 1st through 15th, and the 16th through end-of-month.</summary>
    public static readonly WorkPeriodMode HalfMonth = new("HalfMonth");

    /// <summary>A single period covering the whole calendar month.</summary>
    public static readonly WorkPeriodMode Monthly = new("Monthly");

    /// <summary>A seven-day period.</summary>
    public static readonly WorkPeriodMode Weekly = new("Weekly");

    /// <summary>A fourteen-day period.</summary>
    public static readonly WorkPeriodMode BiWeekly = new("BiWeekly");

    /// <summary>All defined modes, used for lookups and rehydration.</summary>
    public static readonly IReadOnlyList<WorkPeriodMode> All = [HalfMonth, Monthly, Weekly, BiWeekly];

    /// <summary>The persisted string representation of the mode.</summary>
    public string Value { get; }

    private WorkPeriodMode(string value) => Value = value;

    /// <summary>
    /// Rehydrates a <see cref="WorkPeriodMode"/> from its persisted string value.
    /// Falls back to <see cref="HalfMonth"/> when the value is null, empty, or unrecognized so
    /// existing rows without an explicit mode keep legacy behavior.
    /// </summary>
    public static WorkPeriodMode FromValue(string? value) =>
        All.FirstOrDefault(m => m.Value == value) ?? HalfMonth;

    public override string ToString() => Value;
}
