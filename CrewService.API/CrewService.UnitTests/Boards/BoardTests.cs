using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Boards;

public class ExtraBoardTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var board = ExtraBoard.Create(10, 1, "PRIMARY", "Main Board");

        Assert.Equal("PRIMARY", board.BoardKind);
        Assert.Equal("Main Board", board.Name);
        Assert.True(board.IsActive);
        Assert.Null(board.AuxBoardType);
        Assert.True(board.DomainEvents.Count > 0);
    }

    [Fact]
    public void Create_WithAuxBoardType_SetsValue()
    {
        var board = ExtraBoard.Create(10, 1, "AUXILIARY", "Aux Board",
            auxBoardType: "RELIEF");

        Assert.Equal("RELIEF", board.AuxBoardType);
    }

    [Fact]
    public void Update_ChangesProperties()
    {
        var board = ExtraBoard.Create(10, 1, "PRIMARY", "Old Name");

        board.Update("New Name", false, "RELIEF");

        Assert.Equal("New Name", board.Name);
        Assert.False(board.IsActive);
        Assert.Equal("RELIEF", board.AuxBoardType);
    }
}

public class BoardMemberTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var start = DateTime.UtcNow;
        var member = BoardMember.Create(1, 100, 3, start);

        Assert.Equal(1, member.ExtraBoardCtrlNbr.Value);
        Assert.Equal(100, member.EmployeeCtrlNbr.Value);
        Assert.Equal(3, member.OrderIndex);
        Assert.Equal(start, member.StartUtc);
        Assert.Null(member.EndUtc);
    }
}

public class RosterBoardTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), "Main Roster");

        Assert.Equal("Main Roster", board.Name);
        Assert.True(board.IsActive);
        Assert.Empty(board.Positions);
        Assert.True(board.DomainEvents.Count > 0);
    }

    [Fact]
    public void AddPosition_AddsToCollection()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), "Main Roster");

        var pos = board.AddPosition(ControlNumber.Create(100), 1);

        Assert.Single(board.Positions);
        Assert.Equal(100, pos.EmployeeCtrlNbr.Value);
        Assert.Equal(1, pos.PositionOrder);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), "Main Roster");

        board.Deactivate();

        Assert.False(board.IsActive);
    }
}

public class RosterBoardPositionTests
{
    [Fact]
    public void Hangout_SetsHungOutStatus()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), "Test");
        var pos = board.AddPosition(ControlNumber.Create(100), 1);

        pos.Hangout();

        Assert.Equal("HungOut", pos.HangoutStatus);
        Assert.NotNull(pos.HangoutAtUtc);
    }

    [Fact]
    public void MarkOff_SetsMarkedOffStatus()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), "Test");
        var pos = board.AddPosition(ControlNumber.Create(100), 1);

        pos.MarkOff();

        Assert.Equal("MarkedOff", pos.HangoutStatus);
    }

    [Fact]
    public void RestoreFromHangout_ResetsToActive()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(1), ControlNumber.Create(10), "Test");
        var pos = board.AddPosition(ControlNumber.Create(100), 1);
        pos.Hangout();

        pos.RestoreFromHangout();

        Assert.Equal("Active", pos.HangoutStatus);
        Assert.Null(pos.HangoutAtUtc);
    }
}

public class BoardCascadePolicyTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var policy = BoardCascadePolicy.Create(1, 2, "UP_HIERARCHY", 3, true, 2, "SENIORITY");

        Assert.Equal(1, policy.WorkAreaGroupCtrlNbr.Value);
        Assert.Equal(2, policy.CraftCtrlNbr.Value);
        Assert.Equal("UP_HIERARCHY", policy.CascadeMode);
        Assert.Equal(3, policy.MaxLevels);
        Assert.True(policy.AuxEnabled);
        Assert.Equal(2, policy.AuxMaxLevels);
        Assert.Equal("SENIORITY", policy.SelectionStrategy);
    }

    [Fact]
    public void Create_WithNullOptionals_DefaultsCorrectly()
    {
        var policy = BoardCascadePolicy.Create(1, 2, "UP_HIERARCHY", null, false, null, null);

        Assert.Null(policy.MaxLevels);
        Assert.False(policy.AuxEnabled);
        Assert.Null(policy.AuxMaxLevels);
        Assert.Null(policy.SelectionStrategy);
    }
}
