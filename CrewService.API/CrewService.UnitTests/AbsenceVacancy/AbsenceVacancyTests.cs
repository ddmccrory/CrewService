using CrewService.Domain.Modules.AbsenceVacancy;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.AbsenceVacancy;

public class AbsenceCodeTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var code = AbsenceCode.Create("VAC", "Vacation", true, true, true, false, false, 8m, true);

        Assert.Equal("VAC", code.Code);
        Assert.True(code.IsExcused);
        Assert.True(code.IsCompensated);
        Assert.True(code.RequiresApproval);
        Assert.Equal(8m, code.DefaultAutoMarkUpHours);
    }

    [Fact]
    public void Update_ChangesOnlySpecifiedFields()
    {
        var code = AbsenceCode.Create("VAC", "Vacation", true, true, true, false, false, 8m, true);

        code.Update(description: "PTO", isActive: false);

        Assert.Equal("PTO", code.Description);
        Assert.False(code.IsActive);
        Assert.True(code.IsExcused);
    }
}

public class AbsenceRequestTests
{
    [Fact]
    public void Create_DefaultsToPending()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        Assert.Equal("PENDING", request.Status);
        Assert.Equal("VAC", request.ReasonCode);
        Assert.Null(request.ApprovedByCtrlNbr);
        Assert.True(request.DomainEvents.Count > 0);
    }

    [Fact]
    public void Approve_SetsApprovedStatus()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Approve(200);

        Assert.Equal("APPROVED", request.Status);
        Assert.Equal(200, request.ApprovedByCtrlNbr!.Value);
    }

    [Fact]
    public void Deny_SetsDeniedStatus()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Deny(200);

        Assert.Equal("DENIED", request.Status);
    }

    [Fact]
    public void Cancel_SetsCancelledStatus()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        request.Cancel();

        Assert.Equal("CANCELLED", request.Status);
    }

    [Fact]
    public void CompleteByMarkUp_SetsCompletedAndEndUtc()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");
        var markUpTime = DateTime.UtcNow.AddHours(8);

        request.CompleteByMarkUp(markUpTime);

        Assert.Equal("COMPLETED", request.Status);
        Assert.Equal(markUpTime, request.EndUtc);
    }

    [Fact]
    public void AddApproval_AddsToCollection()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");

        var approval = request.AddApproval(ControlNumber.Create(200));

        Assert.Single(request.Approvals);
        Assert.Equal(200, approval.ApprovalOfficerCtrlNbr.Value);
    }

    [Fact]
    public void AddMarkUp_AddsToCollection()
    {
        var request = AbsenceRequest.Create(100, DateTime.UtcNow, null, "VAC");
        var scheduledTime = DateTime.UtcNow.AddHours(8);

        var markUp = request.AddMarkUp(scheduledTime, true);

        Assert.Single(request.MarkUps);
        Assert.True(markUp.IsAutoMarkUp);
    }
}

public class AbsenceCodeCraftOverrideTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var ov = AbsenceCodeCraftOverride.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), 12m);

        Assert.Equal(1, ov.AbsenceCodeCtrlNbr.Value);
        Assert.Equal(10, ov.CraftCtrlNbr.Value);
        Assert.Equal(12m, ov.OverrideAutoMarkUpHours);
    }
}

public class CompensationBalanceTests
{
    [Fact]
    public void Create_SetsInitialBalance()
    {
        var balance = CompensationBalance.Create(
            ControlNumber.Create(100), "VACATION", 80m);

        Assert.Equal("VACATION", balance.CompensationType);
        Assert.Equal(80m, balance.BalanceHours);
    }

    [Fact]
    public void Debit_SufficientBalance_ReturnsTrue()
    {
        var balance = CompensationBalance.Create(
            ControlNumber.Create(100), "VACATION", 80m);

        var result = balance.Debit(8m);

        Assert.True(result);
        Assert.Equal(72m, balance.BalanceHours);
    }

    [Fact]
    public void Debit_InsufficientBalance_ReturnsFalse()
    {
        var balance = CompensationBalance.Create(
            ControlNumber.Create(100), "VACATION", 4m);

        var result = balance.Debit(8m);

        Assert.False(result);
        Assert.Equal(4m, balance.BalanceHours);
    }

    [Fact]
    public void Credit_IncreasesBalance()
    {
        var balance = CompensationBalance.Create(
            ControlNumber.Create(100), "VACATION", 80m);

        balance.Credit(16m);

        Assert.Equal(96m, balance.BalanceHours);
    }
}
