using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Bulletins;

public class PositionVacancyTests
{
    [Fact]
    public void Create_DefaultsToOpen()
    {
        var vacancy = PositionVacancy.Create(99, StaffablePositionType.Crew, 1, 10, "RESIGNATION");

        Assert.Equal("Open", vacancy.Status);
        Assert.Equal(StaffablePositionType.Crew, vacancy.TargetType);
        Assert.Equal("RESIGNATION", vacancy.VacancyReasonCode);
        Assert.Null(vacancy.ClosedUtc);
        Assert.True(vacancy.DomainEvents.Count > 0);
    }

    [Fact]
    public void MarkBulletined_SetsStatus()
    {
        var vacancy = PositionVacancy.Create(99, StaffablePositionType.Crew, 1, 10, "RESIGNATION");

        vacancy.MarkBulletined();

        Assert.Equal("Bulletined", vacancy.Status);
    }

    [Fact]
    public void Fill_SetsFilledAndClosedUtc()
    {
        var vacancy = PositionVacancy.Create(99, StaffablePositionType.Crew, 1, 10, "RESIGNATION");

        vacancy.Fill();

        Assert.Equal("Filled", vacancy.Status);
        Assert.NotNull(vacancy.ClosedUtc);
    }

    [Fact]
    public void Abolish_SetsAbolishedAndClosedUtc()
    {
        var vacancy = PositionVacancy.Create(99, StaffablePositionType.Crew, 1, 10, "RESIGNATION");

        vacancy.Abolish();

        Assert.Equal("Abolished", vacancy.Status);
        Assert.NotNull(vacancy.ClosedUtc);
    }
}

public class BulletinTests
{
    [Fact]
    public void Create_DefaultsToPosted()
    {
        var bulletin = Bulletin.Create(1, 10,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(9));

        Assert.Equal("Posted", bulletin.Status);
        Assert.Null(bulletin.AwardedEmployeeCtrlNbr);
        Assert.True(bulletin.DomainEvents.Count > 0);
    }

    [Fact]
    public void Award_SetsAwardedStatus()
    {
        var bulletin = Bulletin.Create(1, 10,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(9));

        bulletin.Award(100);

        Assert.Equal("Awarded", bulletin.Status);
        Assert.Equal(PositionAssignmentType.BulletinAssignment, bulletin.AwardType);
        Assert.Equal(100, bulletin.AwardedEmployeeCtrlNbr!.Value);
    }

    [Fact]
    public void ForceAssign_SetsForcedStatus()
    {
        var bulletin = Bulletin.Create(1, 10,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(9));

        bulletin.ForceAssign(200);

        Assert.Equal("Forced", bulletin.Status);
        Assert.Equal(PositionAssignmentType.ForceAssignment, bulletin.AwardType);
    }

    [Fact]
    public void Close_SetsClosedStatus()
    {
        var bulletin = Bulletin.Create(1, 10,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(9));

        bulletin.Close();

        Assert.Equal("Closed", bulletin.Status);
    }

    [Fact]
    public void Cancel_SetsCancelledStatus()
    {
        var bulletin = Bulletin.Create(1, 10,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(9));

        bulletin.Cancel();

        Assert.Equal("Cancelled", bulletin.Status);
    }

    [Fact]
    public void IsBidWindowOpen_FalseBeforeWindowOpens()
    {
        var opens = new DateTime(2025, 6, 10, 8, 0, 0, DateTimeKind.Utc);
        var bulletin = Bulletin.Create(1, 10, opens, opens.AddDays(1), opens.AddDays(3));

        Assert.False(bulletin.IsBidWindowOpen(opens.AddMinutes(-1)));
    }

    [Fact]
    public void HasBidWindowOpened_FalseBeforeOpenTime()
    {
        var opens = new DateTime(2025, 6, 10, 8, 0, 0, DateTimeKind.Utc);
        var bulletin = Bulletin.Create(1, 10, opens, opens.AddDays(1), opens.AddDays(3));

        Assert.False(bulletin.HasBidWindowOpened(opens.AddMinutes(-1)));
    }

    [Fact]
    public void HasBidWindowOpened_TrueAtAndAfterOpenTime_IncludingAfterClose()
    {
        var opens = new DateTime(2025, 6, 10, 8, 0, 0, DateTimeKind.Utc);
        var closes = opens.AddDays(1);
        var bulletin = Bulletin.Create(1, 10, opens, closes, closes.AddDays(2));

        Assert.True(bulletin.HasBidWindowOpened(opens));
        Assert.True(bulletin.HasBidWindowOpened(opens.AddHours(6)));
        // Still "opened" even after it has closed — history remains visible to employees.
        Assert.True(bulletin.HasBidWindowOpened(closes.AddDays(1)));
    }

    [Fact]
    public void IsBidWindowOpen_TrueWithinWindowInclusiveOfBounds()
    {
        var opens = new DateTime(2025, 6, 10, 8, 0, 0, DateTimeKind.Utc);
        var closes = opens.AddDays(1);
        var bulletin = Bulletin.Create(1, 10, opens, closes, closes.AddDays(2));

        Assert.True(bulletin.IsBidWindowOpen(opens));
        Assert.True(bulletin.IsBidWindowOpen(opens.AddHours(6)));
        Assert.True(bulletin.IsBidWindowOpen(closes));
    }

    [Fact]
    public void IsBidWindowOpen_FalseAfterWindowCloses()
    {
        var opens = new DateTime(2025, 6, 10, 8, 0, 0, DateTimeKind.Utc);
        var closes = opens.AddDays(1);
        var bulletin = Bulletin.Create(1, 10, opens, closes, closes.AddDays(2));

        Assert.False(bulletin.IsBidWindowOpen(closes.AddMinutes(1)));
    }

    [Fact]
    public void IsBiddable_FalseBeforeWindowOpens_EvenWhenPosted()
    {
        var opens = DateTime.UtcNow.AddHours(1);
        var bulletin = Bulletin.Create(1, 10, opens, opens.AddDays(1), opens.AddDays(3));

        Assert.Equal("Posted", bulletin.Status);
        Assert.False(bulletin.IsBiddable(DateTime.UtcNow));
    }

    [Fact]
    public void IsBiddable_TrueWhenPostedAndWindowOpen()
    {
        var opens = DateTime.UtcNow.AddMinutes(-30);
        var bulletin = Bulletin.Create(1, 10, opens, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(3));

        Assert.True(bulletin.IsBiddable(DateTime.UtcNow));
    }

    [Fact]
    public void IsBiddable_FalseWhenNotPosted_EvenIfWindowOpen()
    {
        var opens = DateTime.UtcNow.AddMinutes(-30);
        var bulletin = Bulletin.Create(1, 10, opens, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(3));
        bulletin.Close();

        Assert.False(bulletin.IsBiddable(DateTime.UtcNow));
    }
}

public class BulletinBidTests
{
    private static readonly DateTime TestSeniorityDate = new(2016, 6, 30);

    [Fact]
    public void Create_DefaultsToSubmitted()
    {
        var bid = BulletinBid.Create(1, 100, 1, TestSeniorityDate, 5);

        Assert.Equal("Submitted", bid.Status);
        Assert.Equal(1, bid.Priority);
        Assert.Equal(TestSeniorityDate, bid.SeniorityDate);
        Assert.Equal(5, bid.SeniorityRank);
    }

    [Fact]
    public void MarkWinner_SetsWinnerStatus()
    {
        var bid = BulletinBid.Create(1, 100, 1, TestSeniorityDate, 5);

        bid.MarkWinner();

        Assert.Equal("Winner", bid.Status);
    }

    [Fact]
    public void MarkLoser_SetsLoserStatus()
    {
        var bid = BulletinBid.Create(1, 100, 1, TestSeniorityDate, 5);

        bid.MarkLoser();

        Assert.Equal("Loser", bid.Status);
    }

    [Fact]
    public void Withdraw_SetsWithdrawnStatus()
    {
        var bid = BulletinBid.Create(1, 100, 1, TestSeniorityDate, 5);

        bid.Withdraw();

        Assert.Equal("Withdrawn", bid.Status);
        Assert.True(bid.DomainEvents.Count > 0);
    }
}

public class BulletinRuleTests
{
    private static BulletinRule MakeRule(
        int bidWindowHours = 72,
        int startHour = 8,
        int closeHour = 16,
        int effectiveOffsetDays = 1,
        int effectiveHour = 6,
        int forceAssignHours = 4) =>
        BulletinRule.Create(
            ControlNumber.Create(100),
            bidWindowHours,
            TimeSpan.FromHours(startHour),
            TimeSpan.FromHours(closeHour),
            effectiveOffsetDays,
            TimeSpan.FromHours(effectiveHour),
            forceAssignHours);

    [Fact]
    public void Create_SetsAllProperties()
    {
        var rule = MakeRule();

        Assert.Equal(100, rule.CraftCtrlNbr.Value);
        Assert.Equal(72, rule.BidWindowHours);
        Assert.Equal(TimeSpan.FromHours(8), rule.BidWindowStartTime);
        Assert.Equal(TimeSpan.FromHours(16), rule.BidWindowCloseTime);
        Assert.Equal(1, rule.EffectiveOffsetDays);
        Assert.Equal(TimeSpan.FromHours(6), rule.EffectiveTime);
        Assert.Equal(4, rule.ForceAssignHours);
        Assert.True(rule.DomainEvents.Count > 0);
    }

    [Fact]
    public void Update_ChangesProperties()
    {
        var rule = MakeRule();

        rule.Update(48, TimeSpan.FromHours(7), TimeSpan.FromHours(15), 2, TimeSpan.FromHours(8), 2);

        Assert.Equal(48, rule.BidWindowHours);
        Assert.Equal(TimeSpan.FromHours(7), rule.BidWindowStartTime);
        Assert.Equal(TimeSpan.FromHours(15), rule.BidWindowCloseTime);
        Assert.Equal(2, rule.EffectiveOffsetDays);
        Assert.Equal(TimeSpan.FromHours(8), rule.EffectiveTime);
        Assert.Equal(2, rule.ForceAssignHours);
    }

    [Fact]
    public void CalculateBidWindow_OpensAtStartTimeOnVacancyDate()
    {
        var rule = MakeRule(bidWindowHours: 24, startHour: 8, closeHour: 16);
        var vacancy = new DateTime(2025, 6, 10, 14, 0, 0, DateTimeKind.Utc);

        var (opens, _, _) = rule.CalculateBidWindow(vacancy);

        Assert.Equal(new DateTime(2025, 6, 10, 8, 0, 0, DateTimeKind.Utc), opens);
    }

    [Fact]
    public void CalculateBidWindow_ClosesAtConfiguredCloseTimeOnClosingDay()
    {
        // 72-hour window from 08:00 on June 10 → raw close June 13 08:00 → snapped to June 13 16:00
        var rule = MakeRule(bidWindowHours: 72, startHour: 8, closeHour: 16);
        var vacancy = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        var (_, closes, _) = rule.CalculateBidWindow(vacancy);

        Assert.Equal(new DateTime(2025, 6, 13, 16, 0, 0, DateTimeKind.Utc), closes);
    }

    [Fact]
    public void CalculateBidWindow_EffectiveDateIsOffsetDaysAfterCloseAtEffectiveTime()
    {
        var rule = MakeRule(bidWindowHours: 72, startHour: 8, closeHour: 16, effectiveOffsetDays: 2, effectiveHour: 6);
        var vacancy = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        var (_, closes, effective) = rule.CalculateBidWindow(vacancy);

        // Close date June 13 + 2 days = June 15 at 06:00
        Assert.Equal(closes.Date.AddDays(2) + TimeSpan.FromHours(6), effective);
    }

    [Fact]
    public void CalculateBidWindow_ZeroOffsetDays_EffectiveOnSameDayAsClose()
    {
        var rule = MakeRule(bidWindowHours: 24, startHour: 8, closeHour: 16, effectiveOffsetDays: 0, effectiveHour: 0);
        var vacancy = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        var (_, closes, effective) = rule.CalculateBidWindow(vacancy);

        Assert.Equal(closes.Date, effective.Date);
    }

    [Fact]
    public void CalculateBidWindow_OpensBeforeCloses()
    {
        var rule = MakeRule();
        var vacancy = DateTime.UtcNow;

        var (opens, closes, _) = rule.CalculateBidWindow(vacancy);

        Assert.True(opens < closes);
    }

    [Fact]
    public void CalculateBidWindow_ClosesBeforeEffective()
    {
        var rule = MakeRule(effectiveOffsetDays: 1);
        var vacancy = DateTime.UtcNow;

        var (_, closes, effective) = rule.CalculateBidWindow(vacancy);

        Assert.True(closes <= effective);
    }
}