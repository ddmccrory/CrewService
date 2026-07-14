using System.Globalization;
using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Time;

/// <summary>
/// Default <see cref="IWorkAreaClock"/> implementation backed by the configured
/// <see cref="TimeProvider"/> and the orchestration unit of work for timezone lookups.
/// </summary>
public sealed class WorkAreaClock(TimeProvider timeProvider, IOrchestrationUnitOfWorkFactory uowFactory) : IWorkAreaClock
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return null;
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { return null; }
        catch (InvalidTimeZoneException) { return null; }
    }

    public DateTimeOffset CombineLocalToUtc(DateOnly localDate, TimeOnly localTime, TimeZoneInfo? tz)
    {
        var naive = localDate.ToDateTime(localTime, DateTimeKind.Unspecified);
        if (tz is null)
            return new DateTimeOffset(DateTime.SpecifyKind(naive, DateTimeKind.Utc));

        // Carry the work area's offset for this local instant. .UtcDateTime still yields the true
        // UTC instant for storage/scheduling, while ToString("O") preserves the local wall clock
        // (e.g. "...-05:00") so the UI can display the correct work-area time without re-converting.
        var offset = tz.GetUtcOffset(naive);
        return new DateTimeOffset(naive, offset);
    }

    public string FormatLocalIso(DateTime utc, TimeZoneInfo? tz)
    {
        if (tz is null) return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
        return local.ToString("O", CultureInfo.InvariantCulture);
    }

    public DateTime ParseToUtc(string value, TimeZoneInfo? tz)
    {
        var dt = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (tz is null || dt.Kind == DateTimeKind.Utc) return dt.ToUniversalTime();
        // Input is local to the work area — strip any kind and convert using its zone.
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), tz);
    }

    public async Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(ControlNumber workAreaCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await ResolveWorkAreaTimeZoneAsync(uow, workAreaCtrlNbr, ct);
    }

    public Task<TimeZoneInfo?> GetWorkAreaTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber workAreaCtrlNbr, CancellationToken ct = default)
        => ResolveWorkAreaTimeZoneAsync(uow, workAreaCtrlNbr, ct);

    public async Task<TimeZoneInfo?> GetCrewTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber crewCtrlNbr, CancellationToken ct = default)
    {
        var crew = await uow.Crews.GetByCtrlNbrAsync(crewCtrlNbr, ct);
        if (crew is null) return null;
        return await ResolveWorkAreaTimeZoneAsync(uow, crew.WorkAreaCtrlNbr, ct);
    }

    private async Task<TimeZoneInfo?> ResolveWorkAreaTimeZoneAsync(IOrchestrationUnitOfWork uow, ControlNumber workAreaCtrlNbr, CancellationToken ct)
    {
        var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(workAreaCtrlNbr, ct);
        return ResolveTimeZone(workArea?.TimeZoneId);
    }
}
