using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.FraCompliance;
using Xunit;

namespace CrewService.UnitTests.FraCompliance;

public class FraRestValidatorTests
{
    private static RegulatoryStandard MakeStandard() =>
        RegulatoryStandard.Create("CFR-228-TRAIN", "Train", 720, 600, true, 6, 7, 2880, 4320, 16560, 1800, 240, new DateOnly(2009, 7, 16));

    [Fact]
    public void CalculateRestRequirement_NormalTour_Returns10hBase()
    {
        var result = new FraRestValidator().CalculateRestRequirement(MakeStandard(), 600);
        Assert.Equal(600, result.BaseRestMinutes);
        Assert.Equal(0, result.PenaltyMinutes);
        Assert.Equal(600, result.TotalRestMinutes);
    }

    [Fact]
    public void CalculateRestRequirement_ExcessTour_AddsPenalty()
    {
        var result = new FraRestValidator().CalculateRestRequirement(MakeStandard(), 780); // 13h
        Assert.Equal(60, result.ExcessMinutes);
        Assert.Equal(60, result.PenaltyMinutes);
        Assert.Equal(660, result.TotalRestMinutes); // 10h + 1h penalty
    }

    [Fact]
    public void IsQuickTieUp_Within3Min_ReturnsTrue()
    {
        Assert.True(new FraRestValidator().IsQuickTieUp(MakeStandard(), 718));
    }

    [Fact]
    public void IsQuickTieUp_WellUnderMax_ReturnsFalse()
    {
        Assert.False(new FraRestValidator().IsQuickTieUp(MakeStandard(), 600));
    }
}
