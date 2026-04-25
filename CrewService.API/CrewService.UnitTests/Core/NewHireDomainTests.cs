using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Core;

public class RosterTypeTests
{
    [Fact]
    public void Create_DefaultsToActive()
    {
        var roster = Roster.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), null,
            "Engineers", "Engineers", 1);

        Assert.Equal(RosterType.Active, roster.RosterType);
    }

    [Fact]
    public void Create_WithTrainingType_SetsTraining()
    {
        var roster = Roster.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), null,
            "Eng Trainees", "Eng Trainees", 99,
            RosterType.Training);

        Assert.Equal(RosterType.Training, roster.RosterType);
        Assert.Equal(99, roster.RosterNumber);
    }
}

public class EmployeeQualificationStatusTests
{
    private static EmployeeQualification CreatePending()
        => EmployeeQualification.Create(
            ControlNumber.Create(10),
            ControlNumber.Create(20),
            grantedBy: "System");

    [Fact]
    public void Status_WhenNoAchievedAt_IsPending()
    {
        var eq = CreatePending();
        Assert.Equal("Pending", eq.Status);
    }

    [Fact]
    public void Status_WhenAchievedAtInFuture_IsPending()
    {
        var eq = EmployeeQualification.Create(
            ControlNumber.Create(10), ControlNumber.Create(20), "System",
            achievedAtUtc: DateTime.UtcNow.AddDays(1));

        Assert.Equal("Pending", eq.Status);
    }

    [Fact]
    public void Activate_SetsAchievedAt_AndStatusBecomesActive()
    {
        var eq = CreatePending();
        var achieved = DateTime.UtcNow.AddDays(-1);
        var expires = DateTime.UtcNow.AddMonths(12);

        eq.Activate(achieved, expires);

        Assert.Equal("Active", eq.Status);
        Assert.Equal(achieved, eq.AchievedAtUtc);
        Assert.Equal(expires, eq.ExpiresAtUtc);
    }

    [Fact]
    public void Activate_WithExpiredDate_StatusIsExpired()
    {
        var eq = CreatePending();

        eq.Activate(DateTime.UtcNow.AddMonths(-13), DateTime.UtcNow.AddDays(-1));

        Assert.Equal("Expired", eq.Status);
    }

    [Fact]
    public void Activate_WithExpiringSoonDate_StatusIsExpiringSoon()
    {
        var eq = CreatePending();
        var expires = DateTime.UtcNow.AddDays(EmployeeQualification.ExpiringSoonDays - 1);

        eq.Activate(DateTime.UtcNow.AddMonths(-6), expires);

        Assert.Equal("ExpiringSoon", eq.Status);
    }

    [Fact]
    public void Revoke_SetsRevokedStatus_RegardlessOfDates()
    {
        var eq = CreatePending();
        eq.Activate(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddMonths(12));

        eq.Revoke("Policy violation");

        Assert.Equal("Revoked", eq.Status);
        Assert.Equal("Policy violation", eq.RevocationReason);
        Assert.NotNull(eq.RevokedAtUtc);
    }

    [Fact]
    public void Reinstate_ClearsRevocation_StatusBecomesActive()
    {
        var eq = CreatePending();
        eq.Activate(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddMonths(12));
        eq.Revoke("Test");

        eq.Reinstate();

        Assert.Equal("Active", eq.Status);
        Assert.Null(eq.RevokedAtUtc);
    }
}

public class EmployeeCertificationDomainTests
{
    [Fact]
    public void Create_DefaultsToStatusPending()
    {
        var cert = EmployeeCertification.Create(
            ControlNumber.Create(10),
            ControlNumber.Create(50),
            "Yard",
            DateOnly.FromDateTime(DateTime.Today),
            recertificationIntervalMonths: 36);

        Assert.Equal("Pending", cert.Status);
    }

    [Fact]
    public void Create_SetsExpirationDateCorrectly()
    {
        var certDate = new DateOnly(2025, 1, 1);

        var cert = EmployeeCertification.Create(
            ControlNumber.Create(10), ControlNumber.Create(50),
            "Yard", certDate, recertificationIntervalMonths: 36);

        Assert.Equal(new DateOnly(2028, 1, 1), cert.ExpirationDate);
    }

    [Fact]
    public void RecomputeStatus_AllChecksValid_SetsStatusToActive()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cert = EmployeeCertification.Create(
            ControlNumber.Create(10), ControlNumber.Create(50),
            "Yard", today, 36);

        foreach (var checkType in EligibilityCheckStalenessLimits.Days.Keys)
            cert.AddEligibilityCheck(checkType, today, 365, "Pass", "Tester");

        Assert.Equal("Active", cert.Status);
    }
}
