using CrewService.Application.HolidayManagement;
using CrewService.Application.Payroll;
using CrewService.Domain.Modules.HolidayManagement;
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
            new HolidayQualificationContext(ControlNumber.Create(10), true, true, null, null), TestContext.Current.CancellationToken);
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
            new HolidayQualificationContext(ControlNumber.Create(10), true, false, null, null), TestContext.Current.CancellationToken);
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
            new HolidayQualificationContext(ControlNumber.Create(10), false, false, "V1", null), TestContext.Current.CancellationToken);
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
            new HolidayQualificationContext(ControlNumber.Create(10), false, false, "V1", null), TestContext.Current.CancellationToken);
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
            new HolidayQualificationContext(ControlNumber.Create(10), true, false, null, "NR"), TestContext.Current.CancellationToken);
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
        Assert.Equal(16, UsHolidayCatalog.All.Count);
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

    [Fact]
    public void Easter2025_April20()
    {
        var holiday = UsHolidayCatalog.GetByCode("EASTER")!;
        Assert.Equal(new DateOnly(2025, 4, 20), holiday.DateResolver(2025));
    }

    [Fact]
    public void GoodFriday2025_April18()
    {
        var holiday = UsHolidayCatalog.GetByCode("GOOD_FRIDAY")!;
        Assert.Equal(new DateOnly(2025, 4, 18), holiday.DateResolver(2025));
    }

    [Fact]
    public void Easter2026_April5()
    {
        var holiday = UsHolidayCatalog.GetByCode("EASTER")!;
        Assert.Equal(new DateOnly(2026, 4, 5), holiday.DateResolver(2026));
    }

    [Fact]
    public void DayAfterThanksgiving_IsFriday()
    {
        var holiday = UsHolidayCatalog.GetByCode("DAY_AFTER_THANKSGIVING")!;
        var date = holiday.DateResolver(2025);
        Assert.Equal(DayOfWeek.Friday, date.DayOfWeek);
        Assert.Equal(11, date.Month);
    }

    [Fact]
    public void ChristmasEve_December24()
    {
        var holiday = UsHolidayCatalog.GetByCode("CHRISTMAS_EVE")!;
        Assert.Equal(new DateOnly(2025, 12, 24), holiday.DateResolver(2025));
    }

    [Fact]
    public void NewYearsEve_December31()
    {
        var holiday = UsHolidayCatalog.GetByCode("NEW_YEARS_EVE")!;
        Assert.Equal(new DateOnly(2025, 12, 31), holiday.DateResolver(2025));
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
            => Task.FromResult<IReadOnlyList<RailroadHolidaySelection>>(
                [.. selections.Where(s => s.WorkAreaGroupCtrlNbr.Value == workAreaGroupCtrlNbr.Value)]);

        public Task<bool> HasOwnSelectionsAsync(
            ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default)
            => Task.FromResult(selections.Any(s => s.WorkAreaGroupCtrlNbr.Value == workAreaGroupCtrlNbr.Value));
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

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026, ct: TestContext.Current.CancellationToken);
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

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026, ct: TestContext.Current.CancellationToken);
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

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026, ct: TestContext.Current.CancellationToken);
        Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 7, 3), result[0].ObservedDate);
    }

    [Fact]
    public async Task ChildInheritsFromParent_WhenNoOwnSelections()
    {
        // Parent (group 100) has selections, child (group 1) has none
        var selections = new List<RailroadHolidaySelection>
        {
            RailroadHolidaySelection.Create(ControlNumber.Create(100), "CHRISTMAS"),
            RailroadHolidaySelection.Create(ControlNumber.Create(100), "NEW_YEAR"),
        };

        var service = new HolidayAutoGenerationService(
            new FakeSelectionRepo(selections), new FakeHolidayRepo([]));

        var result = await service.GenerateForYearAsync(
            ControlNumber.Create(1), 2026, ControlNumber.Create(100), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, h => h.Name == "Christmas Day");
        Assert.Contains(result, h => h.Name == "New Year's Day");
    }

    [Fact]
    public async Task ChildOverridesParent_WhenOwnSelectionsExist()
    {
        // Parent (100) has 2 selections, child (1) has its own 1 selection
        var selections = new List<RailroadHolidaySelection>
        {
            RailroadHolidaySelection.Create(ControlNumber.Create(100), "CHRISTMAS"),
            RailroadHolidaySelection.Create(ControlNumber.Create(100), "NEW_YEAR"),
            RailroadHolidaySelection.Create(ControlNumber.Create(1), "INDEPENDENCE"),
        };

        var service = new HolidayAutoGenerationService(
            new FakeSelectionRepo(selections), new FakeHolidayRepo([]));

        var result = await service.GenerateForYearAsync(
            ControlNumber.Create(1), 2026, ControlNumber.Create(100), TestContext.Current.CancellationToken);

        // Child's own selection wins — only Independence Day, not parent's Christmas/New Year
        Assert.Single(result);
        Assert.Equal("Independence Day", result[0].Name);
    }

    [Fact]
    public async Task NoParent_NoSelections_ReturnsEmpty()
    {
        var service = new HolidayAutoGenerationService(
            new FakeSelectionRepo([]), new FakeHolidayRepo([]));

        var result = await service.GenerateForYearAsync(ControlNumber.Create(1), 2026, ct: TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }
}
