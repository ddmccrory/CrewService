namespace CrewService.Application.HolidayManagement;

public sealed record UsHolidayDefinition(string Code, string Name, Func<int, DateOnly> DateResolver);

public static class UsHolidayCatalog
{
    public static IReadOnlyList<UsHolidayDefinition> All { get; } =
    [
        new("NEW_YEAR", "New Year's Day", year => new DateOnly(year, 1, 1)),
        new("MLK", "Martin Luther King Jr. Day", year => NthWeekday(year, 1, DayOfWeek.Monday, 3)),
        new("PRESIDENTS", "Presidents' Day", year => NthWeekday(year, 2, DayOfWeek.Monday, 3)),
        new("GOOD_FRIDAY", "Good Friday", year => EasterSunday(year).AddDays(-2)),
        new("EASTER", "Easter Sunday", EasterSunday),
        new("MEMORIAL", "Memorial Day", year => LastWeekday(year, 5, DayOfWeek.Monday)),
        new("JUNETEENTH", "Juneteenth", year => new DateOnly(year, 6, 19)),
        new("INDEPENDENCE", "Independence Day", year => new DateOnly(year, 7, 4)),
        new("LABOR", "Labor Day", year => NthWeekday(year, 9, DayOfWeek.Monday, 1)),
        new("COLUMBUS", "Columbus Day", year => NthWeekday(year, 10, DayOfWeek.Monday, 2)),
        new("VETERANS", "Veterans Day", year => new DateOnly(year, 11, 11)),
        new("THANKSGIVING", "Thanksgiving Day", year => NthWeekday(year, 11, DayOfWeek.Thursday, 4)),
        new("DAY_AFTER_THANKSGIVING", "Day After Thanksgiving", year => NthWeekday(year, 11, DayOfWeek.Thursday, 4).AddDays(1)),
        new("CHRISTMAS_EVE", "Christmas Eve", year => new DateOnly(year, 12, 24)),
        new("CHRISTMAS", "Christmas Day", year => new DateOnly(year, 12, 25)),
        new("NEW_YEARS_EVE", "New Year's Eve", year => new DateOnly(year, 12, 31)),
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

    private static DateOnly EasterSunday(int year)
    {
        // Anonymous Gregorian computus algorithm
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }
}
