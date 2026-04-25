using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.HolidayManagement;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.HolidayManagement;

public sealed class HolidayAutoGenerationService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    /// <summary>
    /// Generates holidays for a work area. If the work area has its own selections, those
    /// are used. Otherwise, falls back to the parent group's selections (inherited).
    /// </summary>
    public async Task<IReadOnlyList<Holiday>> GenerateForYearAsync(
        ControlNumber workAreaGroupCtrlNbr, int year,
        ControlNumber? parentGroupCtrlNbr = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var selections = await ResolveSelectionsAsync(uow, workAreaGroupCtrlNbr, parentGroupCtrlNbr, ct);
        var existingHolidays = await uow.Holidays.GetActiveByWorkAreaAsync(workAreaGroupCtrlNbr, ct);

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

    private static async Task<IReadOnlyList<RailroadHolidaySelection>> ResolveSelectionsAsync(
        IOrchestrationUnitOfWork uow, ControlNumber workAreaGroupCtrlNbr,
        ControlNumber? parentGroupCtrlNbr, CancellationToken ct)
    {
        if (await uow.RailroadHolidaySelections.HasOwnSelectionsAsync(workAreaGroupCtrlNbr, ct))
            return await uow.RailroadHolidaySelections.GetActiveByWorkAreaAsync(workAreaGroupCtrlNbr, ct);

        if (parentGroupCtrlNbr is not null)
            return await uow.RailroadHolidaySelections.GetActiveByWorkAreaAsync(parentGroupCtrlNbr, ct);

        return [];
    }
}

