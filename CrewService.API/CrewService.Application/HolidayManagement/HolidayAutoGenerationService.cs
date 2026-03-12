using CrewService.Application.Payroll;
using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.HolidayManagement;

public interface IRailroadHolidaySelectionRepository
{
    Task<IReadOnlyList<RailroadHolidaySelection>> GetActiveByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);

    Task<bool> HasOwnSelectionsAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}

public sealed class HolidayAutoGenerationService(
    IRailroadHolidaySelectionRepository selectionRepo,
    IHolidayRepository holidayRepo)
{
    /// <summary>
    /// Generates holidays for a work area. If the work area has its own selections, those
    /// are used. Otherwise, falls back to the parent group's selections (inherited).
    /// </summary>
    public async Task<IReadOnlyList<Holiday>> GenerateForYearAsync(
        ControlNumber workAreaGroupCtrlNbr, int year,
        ControlNumber? parentGroupCtrlNbr = null, CancellationToken ct = default)
    {
        var selections = await ResolveSelectionsAsync(workAreaGroupCtrlNbr, parentGroupCtrlNbr, ct);
        var existingHolidays = await holidayRepo.GetActiveByWorkAreaAsync(workAreaGroupCtrlNbr, ct);

        var existingDates = existingHolidays
            .Where(h => h.ObservedDate.Year == year)
            .Select(h => h.ObservedDate)
            .ToHashSet();

        var created = new List<Holiday>();

        foreach (var selection in selections)
        {
            var definition = UsHolidayCatalog.GetByCode(selection.HolidayCode);
            if (definition is null) continue;

            var observedDate = ResolveObservedDate(definition.DateResolver(year));
            if (existingDates.Contains(observedDate)) continue;

            var holiday = Holiday.Create(workAreaGroupCtrlNbr, definition.Name, observedDate);
            created.Add(holiday);
            existingDates.Add(observedDate);
        }

        return created;
    }

    private static DateOnly ResolveObservedDate(DateOnly actual)
    {
        return actual.DayOfWeek switch
        {
            DayOfWeek.Saturday => actual.AddDays(-1),
            DayOfWeek.Sunday => actual.AddDays(1),
            _ => actual
        };
    }

    /// <summary>
    /// Railroad-specific selections take priority. If none exist and a parent group
    /// is provided, the parent's selections are inherited.
    /// </summary>
    private async Task<IReadOnlyList<RailroadHolidaySelection>> ResolveSelectionsAsync(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber? parentGroupCtrlNbr, CancellationToken ct)
    {
        if (await selectionRepo.HasOwnSelectionsAsync(workAreaGroupCtrlNbr, ct))
            return await selectionRepo.GetActiveByWorkAreaAsync(workAreaGroupCtrlNbr, ct);

        if (parentGroupCtrlNbr is not null)
            return await selectionRepo.GetActiveByWorkAreaAsync(parentGroupCtrlNbr, ct);

        return [];
    }
}
