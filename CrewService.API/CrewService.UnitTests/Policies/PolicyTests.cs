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

public class DepartmentReassignmentRuleTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var rule = DepartmentReassignmentRule.Create(
            ControlNumber.Create(10),
            Domain.Modules.Boards.BoardType.Hangout,
            isRequired: true);

        Assert.Equal(10, rule.DepartmentCtrlNbr.Value);
        Assert.Equal(Domain.Modules.Boards.BoardType.Hangout, rule.TargetBoardType);
        Assert.True(rule.IsRequired);
    }

    [Fact]
    public void Update_ChangesProperties()
    {
        var rule = DepartmentReassignmentRule.Create(
            ControlNumber.Create(10),
            Domain.Modules.Boards.BoardType.Hangout,
            isRequired: true);

        rule.Update(Domain.Modules.Boards.BoardType.ExtendedAbsence, isRequired: false);

        Assert.Equal(Domain.Modules.Boards.BoardType.ExtendedAbsence, rule.TargetBoardType);
        Assert.False(rule.IsRequired);
    }
}

public class SeniorityMovePolicyTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var policy = SeniorityMovePolicy.Create(
            ControlNumber.Create(1),
            ControlNumber.Create(10),
            requestHours: 24,
            cancelHours: 4,
            crewToCrewEligibilityDays: 30);

        Assert.Equal(30, policy.CrewToCrewEligibilityDays);
        Assert.Equal(24, policy.RequestHours);
        Assert.Equal(4, policy.CancelHours);
        Assert.True(policy.AutoApprove);
    }

    [Fact]
    public void Create_WithAutoApprove_False()
    {
        var policy = SeniorityMovePolicy.Create(
            ControlNumber.Create(1),
            ControlNumber.Create(10),
            autoApprove: false,
            crewToCrewEligibilityDays: 30);

        Assert.False(policy.AutoApprove);
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var policy = SeniorityMovePolicy.Create(
            ControlNumber.Create(1),
            ControlNumber.Create(10),
            crewToCrewEligibilityDays: 30);

        policy.Update(
            requestHours: 48,
            cancelHours: 8,
            autoApprove: false,
            crewToCrewStrategy: "",
            crewToBoardStrategy: "",
            extraBoardToCrewStrategy: "",
            hangoutToCrewStrategy: "",
            extendedAbsenceToCrewStrategy: "",
            trainingToCrewStrategy: "",
            newHireToCrewStrategy: "",
            willWorkEnabled: false,
            crewToCrewEligibilityDays: 60);

        Assert.Equal(60, policy.CrewToCrewEligibilityDays);
        Assert.Equal(48, policy.RequestHours);
        Assert.Equal(8, policy.CancelHours);
        Assert.False(policy.AutoApprove);
    }
}

public class SeniorityMoveTests
{
    [Fact]
    public void Create_DefaultsToPendingVoluntary()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), ControlNumber.Create(200), 30);

        Assert.Equal(SeniorityMoveStatus.Pending, move.Status);
        Assert.Equal(SeniorityMoveType.Voluntary, move.MoveType);
        Assert.Equal(100, move.EmployeeCtrlNbr.Value);
        Assert.Equal(50, move.TargetPositionCtrlNbr.Value);
        Assert.Equal(200, move.DisplacedEmployeeCtrlNbr!.Value);
        Assert.Equal(30, move.DaysOnCurrentPosition);
        Assert.True(move.DomainEvents.Count > 0);
        Assert.Contains(move.DomainEvents, e => e is SeniorityMoveRequestedDomainEvent);
    }

    [Fact]
    public void Create_WithoutDisplacement_NullDisplaced()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);

        Assert.Null(move.DisplacedEmployeeCtrlNbr);
    }

    [Fact]
    public void Create_ForceAssignType_SetsType()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30, SeniorityMoveType.ForceAssign);

        Assert.Equal(SeniorityMoveType.ForceAssign, move.MoveType);
    }

    [Fact]
    public void Approve_TransitionsToApproved()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);

        move.Approve();

        Assert.Equal(SeniorityMoveStatus.Approved, move.Status);
        Assert.Contains(move.DomainEvents, e => e is SeniorityMoveApprovedDomainEvent);
    }

    [Fact]
    public void Approve_WithEffectiveUtc_SetsDate()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);
        var effective = DateTime.UtcNow.AddDays(3);

        move.Approve(effective);

        Assert.Equal(effective, move.EffectiveUtc);
    }

    [Fact]
    public void Approve_WhenNotPending_Throws()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);
        move.Approve();

        Assert.Throws<InvalidOperationException>(() => move.Approve());
    }

    [Fact]
    public void Reject_TransitionsToRejected()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);

        move.Reject("Insufficient seniority");

        Assert.Equal(SeniorityMoveStatus.Rejected, move.Status);
        Assert.Equal("Insufficient seniority", move.RejectionReason);
        Assert.Contains(move.DomainEvents, e => e is SeniorityMoveRejectedDomainEvent);
    }

    [Fact]
    public void Reject_WhenNotPending_Throws()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);
        move.Approve();

        Assert.Throws<InvalidOperationException>(() => move.Reject("Too late"));
    }

    [Fact]
    public void Cancel_FromPending_TransitionsToCancelled()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);

        move.Cancel("Employee withdrew request");

        Assert.Equal(SeniorityMoveStatus.Cancelled, move.Status);
        Assert.Equal("Employee withdrew request", move.CancellationReason);
        Assert.Contains(move.DomainEvents, e => e is SeniorityMoveCancelledDomainEvent);
    }

    [Fact]
    public void Cancel_FromApproved_TransitionsToCancelled()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);
        move.Approve();

        move.Cancel("Rescinded");

        Assert.Equal(SeniorityMoveStatus.Cancelled, move.Status);
    }

    [Fact]
    public void Cancel_WhenCompleted_Throws()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);
        move.Approve();
        move.Complete();

        Assert.Throws<InvalidOperationException>(() => move.Cancel("Too late"));
    }

    [Fact]
    public void Complete_FromApproved_TransitionsToCompleted()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);
        move.Approve();

        move.Complete();

        Assert.Equal(SeniorityMoveStatus.Completed, move.Status);
        Assert.NotNull(move.EffectiveUtc);
        Assert.Contains(move.DomainEvents, e => e is SeniorityMoveCompletedDomainEvent);
    }

    [Fact]
    public void Complete_WhenNotApproved_Throws()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);

        Assert.Throws<InvalidOperationException>(() => move.Complete());
    }

    [Fact]
    public void Complete_WithPresetEffectiveUtc_PreservesDate()
    {
        var move = SeniorityMove.Create(ControlNumber.Create(1), ControlNumber.Create(100), ControlNumber.Create(10), ControlNumber.Create(50), null, 30);
        var effective = DateTime.UtcNow.AddDays(1);
        move.Approve(effective);

        move.Complete();

        Assert.Equal(effective, move.EffectiveUtc);
    }
}
