using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.FraCompliance;

public class FraMonthlyCapTrackerTests
{
    private static RegulatoryStandard MakeStandard() =>
        RegulatoryStandard.Create("CFR-228-TRAIN", "Train", 720, 600, true, 6, 7, 2880, 4320, 16560, 1800, 240, new DateOnly(2009, 7, 16));

    [Fact]
    public void UpdateAndCheck_UnderCap_NoViolation()
    {
        var accum = FraMonthlyAccumulator.Create(ControlNumber.Create(1), "2025-07");
        var ttod = new TtodResult(600, 0, 0, 0, 0, 600);

        var result = new FraMonthlyCapTracker().UpdateAndCheck(MakeStandard(), accum, ttod);

        Assert.False(result.MonthlyCapExceeded);
        Assert.False(result.DeadheadCapExceeded);
    }

    [Fact]
    public void UpdateAndCheck_ExceedsMonthlyCap_Flagged()
    {
        var accum = FraMonthlyAccumulator.Create(ControlNumber.Create(1), "2025-07");
        // Pre-fill to near cap: 16560 = 276h
        accum.AddTourMinutes(16500, 0, 0, 0);

        var ttod = new TtodResult(120, 0, 0, 0, 0, 120);
        var result = new FraMonthlyCapTracker().UpdateAndCheck(MakeStandard(), accum, ttod);

        Assert.True(result.MonthlyCapExceeded);
        Assert.Equal(16620, result.TotalMinutes);
    }
}
