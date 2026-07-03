using System;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Bulletins;

/// <summary>
/// Verifies the craft-scoped force-assign effective-time strategy on <see cref="BulletinRule"/>,
/// mirroring the legacy <c>RailroadPositionBulletin.AssignDateTime</c> craft switch without
/// hardcoded craft names.
/// </summary>
public class BulletinEffectiveTimeModeTests
{
    private static BulletinRule MakeRule(
        string effectiveTimeMode = BulletinEffectiveTimeMode.FixedEffectiveTime,
        int forceAssignHours = 3) =>
        BulletinRule.Create(
            ControlNumber.Create(100),
            bidWindowHours: 72,
            bidWindowStartTime: TimeSpan.FromHours(4),
            bidWindowCloseTime: TimeSpan.FromHours(4),
            effectiveOffsetDays: 0,
            effectiveTime: TimeSpan.FromHours(4),
            forceAssignHours: forceAssignHours,
            effectiveTimeMode: effectiveTimeMode);

    // Monday–Friday operating schedule (Sunday = bit 0 … Saturday = bit 6).
    private const int MonToFriMask =
        (1 << (int)DayOfWeek.Monday) | (1 << (int)DayOfWeek.Tuesday) |
        (1 << (int)DayOfWeek.Wednesday) | (1 << (int)DayOfWeek.Thursday) |
        (1 << (int)DayOfWeek.Friday);

    private static readonly DateTime SaturdayEffectiveUtc = new(2025, 6, 14, 4, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MondayEffectiveUtc = new(2025, 6, 16, 4, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime BidCloseUtc = new(2025, 6, 13, 16, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FixedEffectiveTime_ReturnsConfiguredEffective_EvenOnOffDay()
    {
        var rule = MakeRule(BulletinEffectiveTimeMode.FixedEffectiveTime);

        var result = rule.CalculateForceAssignEffectiveUtc(
            SaturdayEffectiveUtc, BidCloseUtc, MonToFriMask, new TimeOnly(7, 0));

        Assert.Equal(SaturdayEffectiveUtc, result);
    }

    [Fact]
    public void BidWindowCloseTime_ReturnsBidWindowClose()
    {
        var rule = MakeRule(BulletinEffectiveTimeMode.BidWindowCloseTime);

        var result = rule.CalculateForceAssignEffectiveUtc(
            MondayEffectiveUtc, BidCloseUtc, MonToFriMask, new TimeOnly(7, 0));

        Assert.Equal(BidCloseUtc, result);
    }

    [Fact]
    public void EffectiveTimeUnlessOffDay_OnWorkDay_ReturnsConfiguredEffective()
    {
        // Monday June 16 is a work day → configured effective datetime is used unchanged.
        var rule = MakeRule(BulletinEffectiveTimeMode.EffectiveTimeUnlessOffDay);

        var result = rule.CalculateForceAssignEffectiveUtc(
            MondayEffectiveUtc, BidCloseUtc, MonToFriMask, new TimeOnly(7, 0));

        Assert.Equal(MondayEffectiveUtc, result);
    }

    [Fact]
    public void EffectiveTimeUnlessOffDay_OnOffDay_ReturnsFirstWorkDayOnDutyMinusForceHours()
    {
        // Saturday June 14 is an off day → first work day is Monday June 16;
        // on-duty 07:00 minus forceAssignHours(3) = 04:00 UTC on June 16.
        var rule = MakeRule(BulletinEffectiveTimeMode.EffectiveTimeUnlessOffDay, forceAssignHours: 3);

        var result = rule.CalculateForceAssignEffectiveUtc(
            SaturdayEffectiveUtc, BidCloseUtc, MonToFriMask, new TimeOnly(7, 0));

        Assert.Equal(new DateTime(2025, 6, 16, 4, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void OnDutyMinusForceHours_ReturnsWorkDayOnDutyMinusForceHours()
    {
        // Monday June 16 work day, on-duty 09:00 minus forceAssignHours(2) = 07:00 UTC.
        var rule = MakeRule(BulletinEffectiveTimeMode.OnDutyMinusForceHours, forceAssignHours: 2);

        var result = rule.CalculateForceAssignEffectiveUtc(
            MondayEffectiveUtc, BidCloseUtc, MonToFriMask, new TimeOnly(9, 0));

        Assert.Equal(new DateTime(2025, 6, 16, 7, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ScheduleDependentMode_WithoutScheduleData_FallsBackToConfiguredEffective()
    {
        // No operatingDaysMask / onDutyTime → safe fallback to the configured effective datetime.
        var rule = MakeRule(BulletinEffectiveTimeMode.EffectiveTimeUnlessOffDay);

        var result = rule.CalculateForceAssignEffectiveUtc(SaturdayEffectiveUtc, BidCloseUtc);

        Assert.Equal(SaturdayEffectiveUtc, result);
    }
}

public class BulletinRuleEffectiveTimeModePersistenceTests
{
    private static BulletinRule MakeRule(string effectiveTimeMode = BulletinEffectiveTimeMode.FixedEffectiveTime) =>
        BulletinRule.Create(
            ControlNumber.Create(100),
            bidWindowHours: 72,
            bidWindowStartTime: TimeSpan.FromHours(4),
            bidWindowCloseTime: TimeSpan.FromHours(4),
            effectiveOffsetDays: 0,
            effectiveTime: TimeSpan.FromHours(4),
            forceAssignHours: 3,
            effectiveTimeMode: effectiveTimeMode);

    [Fact]
    public void Create_DefaultsToFixedEffectiveTime()
    {
        var rule = BulletinRule.Create(
            ControlNumber.Create(100), 72, TimeSpan.FromHours(4), TimeSpan.FromHours(4), 0, TimeSpan.FromHours(4), 0);

        Assert.Equal(BulletinEffectiveTimeMode.FixedEffectiveTime, rule.EffectiveTimeMode);
    }

    [Fact]
    public void Create_SetsEffectiveTimeMode()
    {
        var rule = MakeRule(BulletinEffectiveTimeMode.EffectiveTimeUnlessOffDay);

        Assert.Equal(BulletinEffectiveTimeMode.EffectiveTimeUnlessOffDay, rule.EffectiveTimeMode);
    }

    [Fact]
    public void Create_InvalidEffectiveTimeMode_Throws()
    {
        Assert.Throws<ArgumentException>(() => MakeRule("NotARealMode"));
    }

    [Fact]
    public void Update_ChangesEffectiveTimeMode()
    {
        var rule = MakeRule(BulletinEffectiveTimeMode.FixedEffectiveTime);

        rule.Update(48, TimeSpan.FromHours(7), TimeSpan.FromHours(15), 2, TimeSpan.FromHours(8), 2,
            effectiveTimeMode: BulletinEffectiveTimeMode.BidWindowCloseTime);

        Assert.Equal(BulletinEffectiveTimeMode.BidWindowCloseTime, rule.EffectiveTimeMode);
    }

    [Fact]
    public void Update_InvalidEffectiveTimeMode_Throws()
    {
        var rule = MakeRule();

        Assert.Throws<ArgumentException>(() =>
            rule.Update(48, TimeSpan.FromHours(7), TimeSpan.FromHours(15), 2, TimeSpan.FromHours(8), 2,
                effectiveTimeMode: "NotARealMode"));
    }

    [Theory]
    [InlineData(BulletinEffectiveTimeMode.FixedEffectiveTime, true)]
    [InlineData(BulletinEffectiveTimeMode.EffectiveTimeUnlessOffDay, true)]
    [InlineData(BulletinEffectiveTimeMode.OnDutyMinusForceHours, true)]
    [InlineData(BulletinEffectiveTimeMode.BidWindowCloseTime, true)]
    [InlineData("bogus", false)]
    public void IsValid_MatchesKnownModes(string mode, bool expected)
    {
        Assert.Equal(expected, BulletinEffectiveTimeMode.IsValid(mode));
    }
}
