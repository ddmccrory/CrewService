using CrewService.Domain.Modules.Bulletins;
using Xunit;

namespace CrewService.UnitTests.Bulletins;

public class PositionVacancyTests
{
    [Fact]
    public void Create_DefaultsToOpen()
    {
        var vacancy = PositionVacancy.Create("Shift", 1, 10, "RESIGNATION");

        Assert.Equal("Open", vacancy.Status);
        Assert.Equal("Shift", vacancy.TargetType);
        Assert.Equal("RESIGNATION", vacancy.VacancyReasonCode);
        Assert.Null(vacancy.ClosedUtc);
        Assert.True(vacancy.DomainEvents.Count > 0);
    }

    [Fact]
    public void MarkBulletined_SetsStatus()
    {
        var vacancy = PositionVacancy.Create("Shift", 1, 10, "RESIGNATION");

        vacancy.MarkBulletined();

        Assert.Equal("Bulletined", vacancy.Status);
    }

    [Fact]
    public void Fill_SetsFilledAndClosedUtc()
    {
        var vacancy = PositionVacancy.Create("Shift", 1, 10, "RESIGNATION");

        vacancy.Fill();

        Assert.Equal("Filled", vacancy.Status);
        Assert.NotNull(vacancy.ClosedUtc);
    }

    [Fact]
    public void Abolish_SetsAbolishedAndClosedUtc()
    {
        var vacancy = PositionVacancy.Create("Shift", 1, 10, "RESIGNATION");

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
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        Assert.Equal("Posted", bulletin.Status);
        Assert.Null(bulletin.AwardedEmployeeCtrlNbr);
        Assert.True(bulletin.DomainEvents.Count > 0);
    }

    [Fact]
    public void Award_SetsAwardedStatus()
    {
        var bulletin = Bulletin.Create(1, 10,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        bulletin.Award(100);

        Assert.Equal("Awarded", bulletin.Status);
        Assert.Equal("BID", bulletin.AwardType);
        Assert.Equal(100, bulletin.AwardedEmployeeCtrlNbr!.Value);
    }

    [Fact]
    public void ForceAssign_SetsForcedStatus()
    {
        var bulletin = Bulletin.Create(1, 10,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        bulletin.ForceAssign(200);

        Assert.Equal("Forced", bulletin.Status);
        Assert.Equal("FORCED", bulletin.AwardType);
    }

    [Fact]
    public void Close_SetsClosedStatus()
    {
        var bulletin = Bulletin.Create(1, 10,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        bulletin.Close();

        Assert.Equal("Closed", bulletin.Status);
    }

    [Fact]
    public void Cancel_SetsCancelledStatus()
    {
        var bulletin = Bulletin.Create(1, 10,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        bulletin.Cancel();

        Assert.Equal("Cancelled", bulletin.Status);
    }
}

public class BulletinBidTests
{
    [Fact]
    public void Create_DefaultsToSubmitted()
    {
        var bid = BulletinBid.Create(1, 100, 1, 5);

        Assert.Equal("Submitted", bid.Status);
        Assert.Equal(1, bid.Priority);
        Assert.Equal(5, bid.SeniorityRank);
    }

    [Fact]
    public void MarkWinner_SetsWinnerStatus()
    {
        var bid = BulletinBid.Create(1, 100, 1, 5);

        bid.MarkWinner();

        Assert.Equal("Winner", bid.Status);
    }

    [Fact]
    public void MarkLoser_SetsLoserStatus()
    {
        var bid = BulletinBid.Create(1, 100, 1, 5);

        bid.MarkLoser();

        Assert.Equal("Loser", bid.Status);
    }

    [Fact]
    public void Withdraw_SetsWithdrawnStatus()
    {
        var bid = BulletinBid.Create(1, 100, 1, 5);

        bid.Withdraw();

        Assert.Equal("Withdrawn", bid.Status);
        Assert.True(bid.DomainEvents.Count > 0);
    }
}
