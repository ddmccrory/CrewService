using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.FraCompliance;

public class DrugAlcoholTests
{
    [Fact]
    public void TestRecord_AlcoholAbove04_IsViolation()
    {
        var record = DrugAlcoholTestRecord.Create(
            ControlNumber.Create(1), "Random", DateTime.UtcNow,
            alcoholResult: 0.05m, drugResult: null, substancesDetected: null, federalAuthority: true);
        Assert.True(record.IsViolation);
    }

    [Fact]
    public void TestRecord_AlcoholInRemovalRange_NotViolation()
    {
        var record = DrugAlcoholTestRecord.Create(
            ControlNumber.Create(1), "Random", DateTime.UtcNow,
            alcoholResult: 0.03m, drugResult: null, substancesDetected: null, federalAuthority: true);
        Assert.False(record.IsViolation);
        Assert.True(record.IsAlcoholRemovalRange);
    }

    [Fact]
    public void TestRecord_DrugRefused_IsViolation()
    {
        var record = DrugAlcoholTestRecord.Create(
            ControlNumber.Create(1), "Random", DateTime.UtcNow,
            alcoholResult: null, drugResult: "Refused", substancesDetected: null, federalAuthority: true);
        Assert.True(record.IsViolation);
    }

    [Fact]
    public void ImpactHandler_FirstViolation_IneligibleDuringTreatment()
    {
        var current = DrugAlcoholTestRecord.Create(
            ControlNumber.Create(1), "Random", DateTime.UtcNow,
            alcoholResult: 0.05m, drugResult: null, substancesDetected: null, federalAuthority: true);

        var result = new DrugAlcoholCertificationImpactHandler()
            .DetermineIneligibility(current, []);

        Assert.True(result.IsIneligible);
        Assert.Null(result.PeriodMonths); // variable — during treatment
        Assert.Equal(1, result.ViolationCount);
    }

    [Fact]
    public void ImpactHandler_SecondViolation_2YearIneligibility()
    {
        var prior = DrugAlcoholTestRecord.Create(
            ControlNumber.Create(1), "Random", DateTime.UtcNow.AddMonths(-12),
            alcoholResult: 0.05m, drugResult: null, substancesDetected: null, federalAuthority: true);

        var current = DrugAlcoholTestRecord.Create(
            ControlNumber.Create(1), "Random", DateTime.UtcNow,
            alcoholResult: 0.06m, drugResult: null, substancesDetected: null, federalAuthority: true);

        var result = new DrugAlcoholCertificationImpactHandler()
            .DetermineIneligibility(current, [prior]);

        Assert.True(result.IsIneligible);
        Assert.Equal(24, result.PeriodMonths);
        Assert.Equal(2, result.ViolationCount);
    }

    [Fact]
    public void ImpactHandler_ThirdViolation_Permanent()
    {
        var p1 = DrugAlcoholTestRecord.Create(
            ControlNumber.Create(1), "Random", DateTime.UtcNow.AddMonths(-24),
            alcoholResult: 0.05m, drugResult: null, substancesDetected: null, federalAuthority: true);
        var p2 = DrugAlcoholTestRecord.Create(
            ControlNumber.Create(1), "Random", DateTime.UtcNow.AddMonths(-12),
            alcoholResult: 0.06m, drugResult: null, substancesDetected: null, federalAuthority: true);

        var current = DrugAlcoholTestRecord.Create(
            ControlNumber.Create(1), "Random", DateTime.UtcNow,
            alcoholResult: 0.05m, drugResult: null, substancesDetected: null, federalAuthority: true);

        var result = new DrugAlcoholCertificationImpactHandler()
            .DetermineIneligibility(current, [p1, p2]);

        Assert.True(result.IsPermanent);
        Assert.Equal(3, result.ViolationCount);
    }
}
