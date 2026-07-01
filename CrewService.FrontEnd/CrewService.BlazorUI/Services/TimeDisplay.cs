using System.Globalization;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Centralized presentation-time formatting. The server localizes UTC instants to the
/// relevant work-area time zone (via <c>IWorkAreaClock</c>) and emits offset-carrying
/// ISO-8601 strings; the front-end must only render those wall-clock values and must never
/// re-convert into the browser's zone with <c>ToLocalTime()</c>.
/// </summary>
public static class TimeDisplay
{
    /// <summary>
    /// Formats an offset-carrying ISO 8601 string as <c>MM/dd/yyyy h:mm tt</c>, or "—" when
    /// empty. Parsing as <see cref="DateTimeOffset"/> and reading <c>.DateTime</c> preserves the
    /// server-emitted work-area wall clock exactly instead of re-converting to the browser zone.
    /// </summary>
    public static string FormatLocalDateTime(string? iso)
    {
        if (!string.IsNullOrWhiteSpace(iso) &&
            DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dto))
            return dto.DateTime.ToString("MM/dd/yyyy h:mm tt");
        return "\u2014";
    }

    /// <summary>
    /// Formats an offset-carrying ISO 8601 string as <c>MM/dd/yy h:mm tt</c> (two-digit year),
    /// or "—" when empty. Preserves the server-emitted work-area wall clock exactly like
    /// <see cref="FormatLocalDateTime"/>, differing only in the shortened year.
    /// </summary>
    public static string FormatLocalDateTimeShortYear(string? iso)
    {
        if (!string.IsNullOrWhiteSpace(iso) &&
            DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dto))
            return dto.DateTime.ToString("MM/dd/yy HH:mm");
        return "\u2014";
    }

    /// <summary>
    /// Formats an offset-carrying ISO 8601 string as a date-only <c>MM/dd/yyyy</c>, or "—" when
    /// empty. Like <see cref="FormatLocalDateTime"/>, the server-emitted work-area wall clock is
    /// preserved by reading <c>.DateTime</c> instead of re-converting into the browser zone.
    /// </summary>
    public static string FormatLocalDate(string? iso)
    {
        if (!string.IsNullOrWhiteSpace(iso) &&
            DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dto))
            return dto.DateTime.ToString("MM/dd/yyyy");
        return "\u2014";
    }
}
