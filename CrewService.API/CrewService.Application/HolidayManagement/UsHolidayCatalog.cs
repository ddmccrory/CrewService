namespace CrewService.Application.HolidayManagement;

public sealed record UsHolidayDefinition(string Code, string Name, Func<int, DateOnly> DateResolver);

public static class UsHolidayCatalog
{
    public static IReadOnlyList<UsHolidayDefinition> All { get; } =
    [
        new("NEW_YEAR", "New Year's Day", year => new DateOnly(year, 1, 1)),
        new("MLK", "Martin Luther King Jr. Day", year => NthWeekday(year, 1, DayOfWeek.Monday, 3)),
        new("PRESIDENTS", "Presidents' Day", year => NthWeekday(year, 2, DayOfWeek.Monday, 3)),
        new("MEMORIAL", "Memorial Day", year => LastWeekday(year, 5, DayOfWeek.Monday)),
        new("JUNETEENTH", "Juneteenth", year => new DateOnly(year, 6, 19)),
        new("INDEPENDENCE", "Independence Day", year => new DateOnly(year, 7, 4)),
        new("LABOR", "Labor Day", year => NthWeekday(year, 9, DayOfWeek.Monday, 1)),
        new("COLUMBUS", "Columbus Day", year => NthWeekday(year, 10, DayOfWeek.Monday, 2)),
        new("VETERANS", "Veterans Day", year => new DateOnly(year, 11, 11)),
        new("THANKSGIVING", "Thanksgiving Day", year => NthWeekday(year, 11, DayOfWeek.Thursday, 4)),
        new("CHRISTMAS", "Christmas Day", year => new DateOnly(year, 12, 25)),
    ];

    public static UsHolidayDefinition? GetByCode(string code) =>
        All.FirstOrDefault(h => h.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    private static DateOnly NthWeekday(int year, int month, DayOfWeek dayOfWeek, int n)
    {
        var date = new DateOnly(year, month, 1);
        var count = 0;
        while (count < n)
        {
            if (date.DayOfWeek == dayOfWeek) count++;
            if (count < n) date = date.AddDays(1);
        }
        return date;
    }

    private static DateOnly LastWeekday(int year, int month, DayOfWeek dayOfWeek)
    {
        var date = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        while (date.DayOfWeek != dayOfWeek) date = date.AddDays(-1);
        return date;
    }
}
