using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.FraCompliance;

public class FraExcessServiceDetectorTests
{
    private static readonly ControlNumber EmpCtrl = ControlNumber.Create(1);
    private static readonly ControlNumber StdCtrl = ControlNumber.Create(1);

    private static RegulatoryStandard MakeStandard() =>
        RegulatoryStandard.Create("CFR-228-TRAIN", "Train", 720, 600, true, 6, 7, 2880, 4320, 16560, 1800, 240, new DateOnly(2009, 7, 16));

    private static FraDutyTour MakeTour(bool isQuickTieUp = false)
    {
        var tour = FraDutyTour.Create(EmpCtrl, StdCtrl, DateTime.UtcNow.AddHours(-12), 600, 1);
        if (isQuickTieUp) tour.Close(DateTime.UtcNow, 780, 60, "test", true);
        return tour;
    }

    [Fact]
    public void Detect_ExceededMaxOnDuty()
    {
        var ttod = new TtodResult(780, 0, 0, 0, 0, 780);
        var violations = new FraExcessServiceDetector().DetectViolations(
            MakeStandard(), MakeTour(), ttod, null, null, 600, []);
        Assert.Contains("ExceededMaxOnDuty", violations);
    }

    [Fact]
    public void Detect_InsufficientPriorRest()
    {
        var ttod = new TtodResult(600, 0, 0, 0, 0, 600);
        var violations = new FraExcessServiceDetector().DetectViolations(
            MakeStandard(), MakeTour(), ttod, null, null, 500, []);
        Assert.Contains("InsufficientPriorRest", violations);
    }

    [Fact]
    public void Detect_Consecutive6Days()
    {
        var ttod = new TtodResult(600, 0, 0, 0, 0, 600);
        var consec = new ConsecutiveDayResult(true, 2880, 6, true);
        var violations = new FraExcessServiceDetector().DetectViolations(
            MakeStandard(), MakeTour(), ttod, null, consec, 600, []);
        Assert.Contains("ExceededConsecutive6Days", violations);
    }

    [Fact]
    public void Detect_Consecutive7Days()
    {
        var ttod = new TtodResult(600, 0, 0, 0, 0, 600);
        var consec = new ConsecutiveDayResult(true, 4320, 7, true);
        var violations = new FraExcessServiceDetector().DetectViolations(
            MakeStandard(), MakeTour(), ttod, null, consec, 600, []);
        Assert.Contains("ExceededConsecutive7Days", violations);
    }

    [Fact]
    public void Detect_ExceededMonthlyCap()
    {
        var ttod = new TtodResult(600, 0, 0, 0, 0, 600);
        var monthly = new MonthlyCapResult(17000, 0, true, false, 16560, 1800);
        var violations = new FraExcessServiceDetector().DetectViolations(
            MakeStandard(), MakeTour(), ttod, monthly, null, 600, []);
        Assert.Contains("ExceededMonthlyCap", violations);
    }

    [Fact]
    public void Detect_ExceededDeadheadMonthlyCap()
    {
        var ttod = new TtodResult(600, 0, 0, 0, 0, 600);
        var monthly = new MonthlyCapResult(10000, 1900, false, true, 16560, 1800);
        var violations = new FraExcessServiceDetector().DetectViolations(
            MakeStandard(), MakeTour(), ttod, monthly, null, 600, []);
        Assert.Contains("ExceededDeadheadMonthlyCap", violations);
    }

    [Fact]
    public void Detect_ExceededWreckReliefLimit()
    {
        // Wreck limit = 720 + 240 = 960 min
        var ttod = new TtodResult(980, 0, 0, 0, 0, 980);
        var violations = new FraExcessServiceDetector().DetectViolations(
            MakeStandard(), MakeTour(), ttod, null, null, 600, []);
        Assert.Contains("ExceededWreckReliefLimit", violations);
    }

    [Fact]
    public void Detect_CommingledServiceExcess()
    {
        var ttod = new TtodResult(600, 180, 0, 0, 0, 780);
        var violations = new FraExcessServiceDetector().DetectViolations(
            MakeStandard(), MakeTour(), ttod, null, null, 600, []);
        Assert.Contains("CommingledServiceExcess", violations);
    }

    [Fact]
    public void Detect_QuickTieUpWithExcess()
    {
        var tour = MakeTour(isQuickTieUp: true);
        var ttod = new TtodResult(780, 0, 0, 0, 0, 780);
        var violations = new FraExcessServiceDetector().DetectViolations(
            MakeStandard(), tour, ttod, null, null, 600, []);
        Assert.Contains("QuickTieUpWithExcess", violations);
    }

    [Fact]
    public void Detect_NormalTour_NoViolations()
    {
        var ttod = new TtodResult(600, 0, 0, 0, 0, 600);
        var violations = new FraExcessServiceDetector().DetectViolations(
            MakeStandard(), MakeTour(), ttod, null, null, 600, []);
        Assert.Empty(violations);
    }
}
