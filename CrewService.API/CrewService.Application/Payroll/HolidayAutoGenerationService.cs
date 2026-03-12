using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Payroll;

public interface IRailroadHolidaySelectionRepository
{
    Task<IReadOnlyList<RailroadHolidaySelection>> GetActiveByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}

public sealed class HolidayAutoGenerationService(
    IRailroadHolidaySelectionRepository selectionRepo,
    IHolidayRepository holidayRepo)
{
    public async Task<IReadOnlyList<Holiday>> GenerateForYearAsync(
        ControlNumber workAreaGroupCtrlNbr, int year, CancellationToken ct = default)
    {
        var selections = await selectionRepo.GetActiveByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
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
}
