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
