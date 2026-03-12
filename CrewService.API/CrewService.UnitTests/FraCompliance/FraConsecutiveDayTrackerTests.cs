using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.FraCompliance;
using Xunit;

namespace CrewService.UnitTests.FraCompliance;

public class FraConsecutiveDayTrackerTests
{
    private static RegulatoryStandard MakeStandard() =>
        RegulatoryStandard.Create("CFR-228-TRAIN", "Train", 720, 600, true, 6, 7, 2880, 4320, 16560, 1800, 240, new DateOnly(2009, 7, 16));

    [Fact]
    public void CalculateConsecutiveDays_NoPrior_Returns1()
    {
        var result = new FraConsecutiveDayTracker()
            .CalculateConsecutiveDays([], DateTime.UtcNow);
        Assert.Equal(1, result);
    }

    [Fact]
    public void CalculateConsecutiveDays_ThreeConsecutive_Returns3()
    {
        var now = DateTime.UtcNow;
        var prior = new List<DateTime> { now.AddDays(-2), now.AddDays(-1) };
        var result = new FraConsecutiveDayTracker().CalculateConsecutiveDays(prior, now);
        Assert.Equal(3, result);
    }

    [Fact]
    public void Evaluate_6Days_AtHome_Returns48hRest()
    {
        var result = new FraConsecutiveDayTracker().Evaluate(MakeStandard(), 6, isAtHomeTerminal: true);
        Assert.True(result.LimitReached);
        Assert.Equal(2880, result.RequiredRestMinutes);
        Assert.Equal(6, result.Tier);
    }

    [Fact]
    public void Evaluate_7Days_Returns72hRest()
    {
        var result = new FraConsecutiveDayTracker().Evaluate(MakeStandard(), 7, isAtHomeTerminal: false);
        Assert.True(result.LimitReached);
        Assert.Equal(4320, result.RequiredRestMinutes);
        Assert.Equal(7, result.Tier);
    }

    [Fact]
    public void Evaluate_5Days_NoLimitReached()
    {
        var result = new FraConsecutiveDayTracker().Evaluate(MakeStandard(), 5, isAtHomeTerminal: true);
        Assert.False(result.LimitReached);
    }
}
