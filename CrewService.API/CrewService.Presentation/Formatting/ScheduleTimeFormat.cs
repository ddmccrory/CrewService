namespace CrewService.Presentation.Formatting;

/// <summary>
/// Centralized formatting for schedule-of-day times (on-duty / off-duty <see cref="TimeOnly"/>
/// values that carry no time zone). Kept in one place so the 12-hour vs 24-hour choice can later
/// be driven by a parent/railroad-level system setting instead of scattered format literals.
/// </summary>
public static class ScheduleTimeFormat
{
    /// <summary>Formats a schedule time of day as 24-hour <c>HH:mm</c>.</summary>
    public static string Format(TimeOnly time) => time.ToString("HH:mm");
}
