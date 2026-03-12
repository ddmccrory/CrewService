using CrewService.Application.Payroll;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.HolidayPayroll;

public class HolidayTests
{
    [Fact]
    public void Create_SetsActiveByDefault()
    {
        var holiday = Holiday.Create(
            ControlNumber.Create(1), "July 4th", new DateOnly(2025, 7, 4));
        Assert.True(holiday.IsActive);
        Assert.Equal("July 4th", holiday.Name);
    }
}

public class HolidayQualificationServiceTests
{
    private sealed class FakeRuleRepo(List<HolidayQualificationRule> rules) : IHolidayQualificationRuleRepository
    {
        public Task<IReadOnlyList<HolidayQualificationRule>> GetByHolidayAsync(
            ControlNumber holidayCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<HolidayQualificationRule>>(rules);
    }

    [Fact]
    public async Task NoRules_ReturnsQualified()
    {
        var service = new HolidayQualificationService(new FakeRuleRepo([]));
        var result = await service.EvaluateAsync(ControlNumber.Create(1),
            new HolidayQualificationContext(ControlNumber.Create(10), true, true, null, null));
        Assert.True(result.IsQualified);
    }

    [Fact]
    public async Task WorkedDayBefore_Passes()
    {
        var rules = new List<HolidayQualificationRule>
        {
            HolidayQualificationRule.Create(ControlNumber.Create(1), true, false),
        };
        var service = new HolidayQualificationService(new FakeRuleRepo(rules));
        var result = await service.EvaluateAsync(ControlNumber.Create(1),
            new HolidayQualificationContext(ControlNumber.Create(10), true, false, null, null));
        Assert.True(result.IsQualified);
    }

    [Fact]
    public async Task DidNotWorkDayBefore_Fails()
    {
        var rules = new List<HolidayQualificationRule>
        {
            HolidayQualificationRule.Create(ControlNumber.Create(1), true, false),
        };
        var service = new HolidayQualificationService(new FakeRuleRepo(rules));
        var result = await service.EvaluateAsync(ControlNumber.Create(1),
            new HolidayQualificationContext(ControlNumber.Create(10), false, false, "V1", null));
        Assert.False(result.IsQualified);
        Assert.Equal("Did not work day before", result.DisqualificationReason);
    }

    [Fact]
    public async Task DidNotWorkDayBefore_ExemptCode_Passes()
    {
        var rules = new List<HolidayQualificationRule>
        {
            HolidayQualificationRule.Create(ControlNumber.Create(1), true, false, exemptAbsenceCodes: "[\"V1\"]"),
        };
        var service = new HolidayQualificationService(new FakeRuleRepo(rules));
        var result = await service.EvaluateAsync(ControlNumber.Create(1),
            new HolidayQualificationContext(ControlNumber.Create(10), false, false, "V1", null));
        Assert.True(result.IsQualified);
    }

    [Fact]
    public async Task DidNotWorkDayAfter_Fails()
    {
        var rules = new List<HolidayQualificationRule>
        {
            HolidayQualificationRule.Create(ControlNumber.Create(1), false, true),
        };
        var service = new HolidayQualificationService(new FakeRuleRepo(rules));
        var result = await service.EvaluateAsync(ControlNumber.Create(1),
            new HolidayQualificationContext(ControlNumber.Create(10), true, false, null, "NR"));
        Assert.False(result.IsQualified);
        Assert.Equal("Did not work day after", result.DisqualificationReason);
    }
}

public class HolidayPayrollRecordTests
{
    [Fact]
    public void Create_Qualified_NoReason()
    {
        var record = HolidayPayrollRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), true);
        Assert.True(record.IsQualified);
        Assert.Null(record.DisqualificationReason);
    }

    [Fact]
    public void Create_Disqualified_HasReason()
    {
        var record = HolidayPayrollRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), false, "Did not work day before");
        Assert.False(record.IsQualified);
        Assert.Equal("Did not work day before", record.DisqualificationReason);
    }
}

public class UsHolidayCatalogTests
{
    [Fact]
    public void All_Contains11Holidays()
    {
        Assert.Equal(11, UsHolidayCatalog.All.Count);
    }

    [Fact]
    public void GetByCode_ReturnsMatch()
    {
        var holiday = UsHolidayCatalog.GetByCode("INDEPENDENCE");
        Assert.NotNull(holiday);
        Assert.Equal("Independence Day", holiday!.Name);
    }

    [Fact]
    public void GetByCode_Invalid_ReturnsNull()
    {
        Assert.Null(UsHolidayCatalog.GetByCode("FAKE"));
    }

    [Fact]
    public void IndependenceDay_ResolvesJuly4()
    {
        var holiday = UsHolidayCatalog.GetByCode("INDEPENDENCE")!;
        Assert.Equal(new DateOnly(2025, 7, 4), holiday.DateResolver(2025));
    }

    [Fact]
    public void MemorialDay_LastMondayInMay()
    {
        var holiday = UsHolidayCatalog.GetByCode("MEMORIAL")!;
        var date = holiday.DateResolver(2025);
        Assert.Equal(DayOfWeek.Monday, date.DayOfWeek);
        Assert.Equal(5, date.Month);
    }

    [Fact]
    public void Thanksgiving_FourthThursdayInNovember()
    {
        var holiday = UsHolidayCatalog.GetByCode("THANKSGIVING")!;
        var date = holiday.DateResolver(2025);
        Assert.Equal(DayOfWeek.Thursday, date.DayOfWeek);
        Assert.Equal(11, date.Month);
    }
}

public class RailroadHolidaySelectionTests
{
    [Fact]
    public void Create_ActiveByDefault()
    {
        var selection = RailroadHolidaySelection.Create(ControlNumber.Create(1), "CHRISTMAS");
        Assert.True(selection.IsActive);
        Assert.Equal("CHRISTMAS", selection.HolidayCode);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var selection = RailroadHolidaySelection.Create(ControlNumber.Create(1), "CHRISTMAS");
        selection.Deactivate();
        Assert.False(selection.IsActive);
    }

    [Fact]
    public void Activate_SetsActive()
    {
        var selection = RailroadHolidaySelection.Create(ControlNumber.Create(1), "CHRISTMAS", false);
        selection.Activate();
        Assert.True(selection.IsActive);
    }
}

public class HolidayAutoGenerationServiceTests
{
    private sealed class FakeSelectionRepo(List<RailroadHolidaySelection> selections) : IRailroadHolidaySelectionRepository
    {
        public Task<IReadOnlyList<RailroadHolidaySelection>> GetActiveByWorkAreaAsync(
            ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RailroadHolidaySelection>>(selections);
    }

    private sealed class FakeHolidayRepo(List<Holiday> holidays) : IHolidayRepository
    {
        public Task<IReadOnlyList<Holiday>> GetActiveByWorkAreaAsync(
            ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Holiday>>(holidays);
    }

    [Fact]
    public async Task GeneratesHolidaysFromSelections()
    {
        var selections = new List<RailroadHolidaySelection>
        {
            RailroadHolidaySelection.Create(ControlNumber.Create(1), "CHRISTMAS"),
            RailroadHolidaySelection.Create(ControlNumber.Create(1), "NEW_YEAR"),
        };

        var service = new HolidayAutoGenerationService(
            new FakeSelectionRepo(selections), new FakeHolidayRepo([]));

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, h => h.Name == "Christmas Day");
        Assert.Contains(result, h => h.Name == "New Year's Day");
    }

    [Fact]
    public async Task SkipsAlreadyExisting()
    {
        var selections = new List<RailroadHolidaySelection>
        {
            RailroadHolidaySelection.Create(ControlNumber.Create(1), "CHRISTMAS"),
        };
        var existing = new List<Holiday>
        {
            Holiday.Create(ControlNumber.Create(1), "Christmas Day", new DateOnly(2026, 12, 25)),
        };

        var service = new HolidayAutoGenerationService(
            new FakeSelectionRepo(selections), new FakeHolidayRepo(existing));

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026);
        Assert.Empty(result);
    }

    [Fact]
    public async Task WeekendHoliday_ShiftsToObservedDate()
    {
        // July 4, 2026 is a Saturday → observed Friday July 3
        var selections = new List<RailroadHolidaySelection>
        {
            RailroadHolidaySelection.Create(ControlNumber.Create(1), "INDEPENDENCE"),
        };

        var service = new HolidayAutoGenerationService(
            new FakeSelectionRepo(selections), new FakeHolidayRepo([]));

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026);
        Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 7, 3), result[0].ObservedDate);
    }
}
