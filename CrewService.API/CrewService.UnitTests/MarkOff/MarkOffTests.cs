using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.MarkOff;

public class AbsenceCodeTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var code = AbsenceCode.Create("V1", "Vacation Week 1", true, true, true, false, false, 168m, true);
        Assert.Equal("V1", code.Code);
        Assert.True(code.IsCompensated);
        Assert.True(code.RequiresApproval);
        Assert.Equal(168m, code.DefaultAutoMarkUpHours);
    }

    [Fact]
    public void SystemOnlyCodes_CannotBeCreatedByUser()
    {
        var code = AbsenceCode.Create("SR", "Safety Rest", true, false, false, true, false, null, true);
        Assert.True(code.IsSystemOnly);
    }
}

public class AbsenceApprovalTests
{
    [Fact]
    public void AddApproval_CreatesWithPendingStatus()
    {
        var request = AbsenceRequest.CreateWithCode(
            1, DateTime.UtcNow, null, ControlNumber.Create(10), "V1");
        var approval = request.AddApproval(ControlNumber.Create(99));

        Assert.Equal("PENDING", approval.Status);
        Assert.Single(request.Approvals);
    }

    [Fact]
    public void Approve_SetsStatusAndTimestamp()
    {
        var request = AbsenceRequest.CreateWithCode(
            1, DateTime.UtcNow, null, ControlNumber.Create(10), "V1");
        var approval = request.AddApproval(ControlNumber.Create(99));
        approval.Approve("Looks good");

        Assert.Equal("APPROVED", approval.Status);
        Assert.NotNull(approval.DecidedAtUtc);
    }

    [Fact]
    public void Decline_SetsStatusAndTimestamp()
    {
        var request = AbsenceRequest.CreateWithCode(
            1, DateTime.UtcNow, null, ControlNumber.Create(10), "V1");
        var approval = request.AddApproval(ControlNumber.Create(99));
        approval.Decline("Not enough notice");

        Assert.Equal("DECLINED", approval.Status);
        Assert.NotNull(approval.DecidedAtUtc);
    }
}

public class AbsenceMarkUpTests
{
    [Fact]
    public void AddMarkUp_SetsScheduledTime()
    {
        var start = DateTime.UtcNow;
        var request = AbsenceRequest.CreateWithCode(
            1, start, null, ControlNumber.Create(10), "V1");
        var markUp = request.AddMarkUp(start.AddHours(168), true);

        Assert.Equal(start.AddHours(168), markUp.ScheduledMarkUpUtc);
        Assert.True(markUp.IsAutoMarkUp);
        Assert.Null(markUp.ActualMarkUpUtc);
    }

    [Fact]
    public void Execute_SetsActualTime()
    {
        var start = DateTime.UtcNow;
        var request = AbsenceRequest.CreateWithCode(
            1, start, null, ControlNumber.Create(10), "V1");
        var markUp = request.AddMarkUp(start.AddHours(168), true);
        markUp.Execute(start.AddHours(168));

        Assert.NotNull(markUp.ActualMarkUpUtc);
    }
}

public class CompensationBalanceTests
{
    [Fact]
    public void Debit_ReducesBalance()
    {
        var balance = CompensationBalance.Create(ControlNumber.Create(1), "VACATION", 80m);
        var result = balance.Debit(40m);

        Assert.True(result);
        Assert.Equal(40m, balance.BalanceHours);
    }

    [Fact]
    public void Debit_InsufficientBalance_ReturnsFalse()
    {
        var balance = CompensationBalance.Create(ControlNumber.Create(1), "VACATION", 20m);
        var result = balance.Debit(40m);

        Assert.False(result);
        Assert.Equal(20m, balance.BalanceHours);
    }

    [Fact]
    public void Credit_IncreasesBalance()
    {
        var balance = CompensationBalance.Create(ControlNumber.Create(1), "VACATION", 40m);
        balance.Credit(20m);

        Assert.Equal(60m, balance.BalanceHours);
    }
}
