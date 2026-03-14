using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Policies;

public class CraftOperationsPolicyTests
{
    [Fact]
    public void Create_UsesDefaults()
    {
        var policy = CraftOperationsPolicy.Create(ControlNumber.Create(10));

        Assert.Equal(90, policy.LateCallThresholdMinutes);
        Assert.Equal("FRA", policy.RestCalculationStrategy);
        Assert.Null(policy.FixedRestHours);
        Assert.Equal(24m, policy.ConsecutiveDayResetHours);
        Assert.False(policy.DeleteConflictingNextShift);
        Assert.False(policy.AutoAnnulCreatesOffDuty);
    }

    [Fact]
    public void Update_ChangesOnlySpecifiedFields()
    {
        var policy = CraftOperationsPolicy.Create(ControlNumber.Create(10));

        policy.Update(lateCallThresholdMinutes: 60, autoAnnulCreatesOffDuty: true);

        Assert.Equal(60, policy.LateCallThresholdMinutes);
        Assert.True(policy.AutoAnnulCreatesOffDuty);
        Assert.Equal("FRA", policy.RestCalculationStrategy);
    }
}

public class CraftDisplacementPolicyTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var policy = CraftDisplacementPolicy.Create(10, 48, "ROSTER", "EXTRA_BOARD");

        Assert.Equal(48, policy.WindowHours);
        Assert.Equal("ROSTER", policy.SeniorityBasis);
        Assert.Equal("EXTRA_BOARD", policy.DefaultAction);
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var policy = CraftDisplacementPolicy.Create(10, 48, "ROSTER", "EXTRA_BOARD");

        policy.Update(72, "DATE", "FURLOUGH", null);

        Assert.Equal(72, policy.WindowHours);
        Assert.Equal("DATE", policy.SeniorityBasis);
        Assert.Equal("FURLOUGH", policy.DefaultAction);
    }
}

public class DisplacementCaseTests
{
    [Fact]
    public void Create_DefaultsToOpen()
    {
        var now = DateTime.UtcNow;
        var dc = DisplacementCase.Create(100, 10, now, now.AddHours(48));

        Assert.Equal("Open", dc.Status);
        Assert.True(dc.DomainEvents.Count > 0);
    }

    [Fact]
    public void Close_SetsStatus()
    {
        var now = DateTime.UtcNow;
        var dc = DisplacementCase.Create(100, 10, now, now.AddHours(48));

        dc.Close("Resolved");

        Assert.Equal("Resolved", dc.Status);
    }

    [Fact]
    public void AutoPlaceOnExtraBoard_SetsAutoPlaced()
    {
        var now = DateTime.UtcNow;
        var dc = DisplacementCase.Create(100, 10, now, now.AddHours(48));

        dc.AutoPlaceOnExtraBoard();

        Assert.Equal("AutoPlaced", dc.Status);
    }
}

public class DisplacementClaimTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var claim = DisplacementClaim.Create(1, 200, DateTime.UtcNow);

        Assert.Equal(200, claim.TargetEmployeeCtrlNbr.Value);
        Assert.Null(claim.Decision);
    }

    [Fact]
    public void Decide_SetsDecisionAndTimestamp()
    {
        var claim = DisplacementClaim.Create(1, 200, DateTime.UtcNow);

        claim.Decide("Approved", "Senior employee");

        Assert.Equal("Approved", claim.Decision);
        Assert.Equal("Senior employee", claim.Reason);
        Assert.NotNull(claim.DecidedUtc);
    }
}

public class BulletinPolicyTests
{
    [Fact]
    public void Create_UsesDefaults()
    {
        var policy = BulletinPolicy.Create(10, 72);

        Assert.Equal(72, policy.BidWindowHours);
        Assert.True(policy.ForcedAssignmentEnabled);
        Assert.Equal("JUNIOR_FIRST", policy.ForcedAssignmentBasis);
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var policy = BulletinPolicy.Create(10, 72);

        policy.Update(48, false, "SENIOR_FIRST");

        Assert.Equal(48, policy.BidWindowHours);
        Assert.False(policy.ForcedAssignmentEnabled);
        Assert.Equal("SENIOR_FIRST", policy.ForcedAssignmentBasis);
    }
}

public class SeniorityMovePolicyTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var policy = SeniorityMovePolicy.Create(10, 90, "ROSTER");

        Assert.Equal(90, policy.EligibilityDays);
        Assert.Equal("ROSTER", policy.SeniorityBasis);
    }

    [Fact]
    public void Update_ChangesFields()
    {
        var policy = SeniorityMovePolicy.Create(10, 90, "ROSTER");

        policy.Update(60, "DATE");

        Assert.Equal(60, policy.EligibilityDays);
        Assert.Equal("DATE", policy.SeniorityBasis);
    }
}

public class SeniorityMoveTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var move = SeniorityMove.Create(100, 10, 50, 200, 30);

        Assert.Equal(100, move.EmployeeCtrlNbr.Value);
        Assert.Equal(50, move.TargetPositionCtrlNbr.Value);
        Assert.Equal(200, move.DisplacedEmployeeCtrlNbr!.Value);
        Assert.Equal(30, move.DaysOnCurrentPosition);
        Assert.True(move.DomainEvents.Count > 0);
    }

    [Fact]
    public void Create_WithoutDisplacement_NullDisplaced()
    {
        var move = SeniorityMove.Create(100, 10, 50, null, 30);

        Assert.Null(move.DisplacedEmployeeCtrlNbr);
    }
}
