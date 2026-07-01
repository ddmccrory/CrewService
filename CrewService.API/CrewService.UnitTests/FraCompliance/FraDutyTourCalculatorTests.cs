using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.FraCompliance;

public class FraDutyTourCalculatorTests
{
    private static readonly ControlNumber EmpCtrl = ControlNumber.Create(1);
    private static readonly ControlNumber StdCtrl = ControlNumber.Create(1);
    private static readonly ControlNumber OdCtrl = ControlNumber.Create(100);

    [Fact]
    public void Calculate_CoveredServiceOnly_ReturnsCoveredMinutes()
    {
        var tour = FraDutyTour.Create(EmpCtrl, StdCtrl, DateTime.UtcNow.AddHours(-10), 600, 1);
        tour.AddSegment(OdCtrl, "Engineer", "LOC1", DateTime.UtcNow.AddHours(-10));
        tour.Segments[0].Complete("LOC2", DateTime.UtcNow);

        var result = new FraDutyTourCalculator().Calculate(tour);

        Assert.True(result.TotalTimeOnDutyMinutes > 0);
        Assert.Equal(result.CoveredServiceMinutes, result.TotalTimeOnDutyMinutes);
    }

    [Fact]
    public void Calculate_WithDeadheadToAssignment_IncludedInTtod()
    {
        var start = DateTime.UtcNow.AddHours(-12);
        var tour = FraDutyTour.Create(EmpCtrl, StdCtrl, start, 600, 1);
        tour.AddSegment(OdCtrl, "Engineer", "LOC1", start);
        tour.Segments[0].Complete("LOC2", start.AddHours(8));
        tour.AddTransportationSegment("LOC0", start.AddHours(-2), "LOC1", start, "Train", true);

        var result = new FraDutyTourCalculator().Calculate(tour);

        Assert.Equal(120, result.DeadheadToAssignmentMinutes);
        Assert.True(result.TotalTimeOnDutyMinutes > result.CoveredServiceMinutes);
    }

    [Fact]
    public void CalculatePriorTimeOff_NoPreviousTour_ReturnsMaxValue()
    {
        var result = new FraDutyTourCalculator().CalculatePriorTimeOffMinutes(null, DateTime.UtcNow);
        Assert.Equal(int.MaxValue, result);
    }
}
