using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Time;

/// <summary>
/// Centralizes time handling for the application: a deterministic "now" sourced from the
/// configured <see cref="TimeProvider"/>, plus work-area timezone resolution and conversion.
/// <para>
/// Work-area schedule times (on/off duty) are stored as <see cref="TimeOnly"/> wall-clock
/// values in the work area's local zone. This service turns those local values into
/// unambiguous UTC instants and back, replacing the duplicated timezone helpers that
/// previously lived in the presentation services.
/// </para>
/// </summary>
public interface IWorkAreaClock
{
    /// <summary>Current UTC instant from the configured <see cref="TimeProvider"/>.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Resolves a <see cref="TimeZoneInfo"/> from an IANA or Windows timezone id.
    /// Returns <c>null</c> when the id is blank or cannot be resolved, in which case callers
    /// should treat values as already being in UTC (preserving pre-Phase-2 behavior).
    /// </summary>
    TimeZoneInfo? ResolveTimeZone(string? timeZoneId);

    /// <summary>
    /// Combines a work-area-local calendar date and wall-clock time into a true UTC instant.
    /// When <paramref name="tz"/> is <c>null</c> the inputs are treated as already UTC.
    /// </summary>
    DateTimeOffset CombineLocalToUtc(DateOnly localDate, TimeOnly localTime, TimeZoneInfo? tz);

    /// <summary>
    /// Formats a UTC <see cref="DateTime"/> as a round-trip ISO-8601 string in the given zone.
    /// When <paramref name="tz"/> is <c>null</c> the value is rendered as-is (UTC).
    /// </summary>
    string FormatLocalIso(DateTime utc, TimeZoneInfo? tz);

    /// <summary>
    /// Parses an ISO-8601 string into a UTC <see cref="DateTime"/>. A value carrying an explicit
    /// offset (or <c>Z</c>) is honored directly; an unspecified value is interpreted as local to
    /// <paramref name="tz"/> (or UTC when <paramref name="tz"/> is <c>null</c>).
    /// </summary>
    DateTime ParseToUtc(string value, TimeZoneInfo? tz);

    /// <summary>
    /// Resolves the configured <see cref="TimeZoneInfo"/> for a work-area group, opening a
    /// short-lived read unit of work. Returns <c>null</c> when no zone is configured.
    /// </summary>
    Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(ControlNumber workAreaCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// Resolves the configured <see cref="TimeZoneInfo"/> for a work-area group using an existing
    /// unit of work. Returns <c>null</c> when no zone is configured.
    /// </summary>
    Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber workAreaCtrlNbr, CancellationToken ct = default);

    /// <summary>
    /// Resolves the work-area <see cref="TimeZoneInfo"/> for a crew using an existing unit of work,
    /// following the chain <c>Crew.WorkAreaCtrlNbr → DynamicGroup.TimeZoneId</c>.
    /// Returns <c>null</c> when no zone is configured.
    /// </summary>
    Task<TimeZoneInfo?> GetCrewTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber crewCtrlNbr, CancellationToken ct = default);
}
