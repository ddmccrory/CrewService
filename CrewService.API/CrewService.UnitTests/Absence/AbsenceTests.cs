using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Absence;

public class AbsenceCodeTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var code = AbsenceCode.Create(1, "V1", "Vacation Week 1", true, true, true, false, false, 168m, true);
        Assert.Equal("V1", code.Code);
        Assert.True(code.IsCompensated);
        Assert.True(code.RequiresApproval);
        Assert.Equal(168m, code.DefaultAutoMarkUpHours);
    }

    [Fact]
    public void SystemOnlyCodes_CannotBeCreatedByUser()
    {
        var code = AbsenceCode.Create(1, "SR", "Safety Rest", true, false, false, true, false, null, true);
        Assert.True(code.IsSystemOnly);
    }
}

public class AbsenceRequestLifecycleTests
{
    [Fact]
    public void Approve_SetsApprovedMetadata()
    {
        var request = AbsenceRequest.CreateWithCode(
            1, DateTime.UtcNow, null, ControlNumber.Create(10), "V1");
        request.Approve(ControlNumber.Create(99));

        Assert.Equal("APPROVED", request.DerivedStatus);
        Assert.Equal(99, request.ApprovedByCtrlNbr!.Value);
        Assert.NotNull(request.ApprovedAtUtc);
    }

    [Fact]
    public void Approve_DoesNotCompleteRequest()
    {
        var request = AbsenceRequest.CreateWithCode(
            1, DateTime.UtcNow, null, ControlNumber.Create(10), "V1");

        request.Approve(ControlNumber.Create(99));

        Assert.Equal("APPROVED", request.DerivedStatus);
        Assert.Empty(request.EndRecords);
    }

    [Fact]
    public void StartAndEnd_CreateOperationalRecords()
    {
        var start = DateTime.UtcNow;
        var request = AbsenceRequest.CreateWithCode(
            1, start, null, ControlNumber.Create(10), "V1");
        request.Approve(ControlNumber.Create(99));
        request.Start(start);
        request.AddEndRecord(start.AddHours(8), true);

        Assert.Single(request.StartRecords);
        Assert.Single(request.EndRecords);
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
