using CrewService.Domain.DomainEvents.Boards;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.Boards;

public class RosterBoardTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Main Roster");

        Assert.Equal("Main Roster", board.Name);
        Assert.True(board.IsActive);
        Assert.Equal(BoardType.ExtraBoard, board.BoardType);
        Assert.Equal(RotationType.StandardRotation, board.RotationType);
        Assert.Equal(100, board.RosterCtrlNbr.Value);
        Assert.Empty(board.Positions);
        Assert.True(board.DomainEvents.Count > 0);
    }

    [Fact]
    public void Create_WithBoardType_SetsValue()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100),
            "Extra Board", BoardType.ExtraBoard, RotationType.FirstInFirstOut);

        Assert.Equal(BoardType.ExtraBoard, board.BoardType);
        Assert.Equal(RotationType.FirstInFirstOut, board.RotationType);
    }

    [Fact]
    public void AddPosition_AddsToCollection()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Main Roster");

        var pos = board.AddPosition(ControlNumber.Create(200), 1, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);

        Assert.Single(board.Positions);
        Assert.Equal(200, pos.EmployeeCtrlNbr.Value);
        Assert.Equal(1, pos.PositionOrder);
    }

    [Fact]
    public void Update_ChangesProperties()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Old Name");

        board.Update("New Name", BoardType.Training, RotationType.SeniorityBased, false);

        Assert.Equal("New Name", board.Name);
        Assert.Equal(BoardType.Training, board.BoardType);
        Assert.Equal(RotationType.SeniorityBased, board.RotationType);
        Assert.False(board.IsActive);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Main Roster");

        board.Deactivate();

        Assert.False(board.IsActive);
    }

    [Fact]
    public void ReorderPositions_RaisesDomainEventWithChanges()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Test");
        var pos1 = board.AddPosition(ControlNumber.Create(200), 1, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);
        var pos2 = board.AddPosition(ControlNumber.Create(201), 2, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);

        board.ReorderPositions([(pos1.CtrlNbr, 2), (pos2.CtrlNbr, 1)]);

        var evt = Assert.Single(board.DomainEvents.OfType<PositionsReorderedDomainEvent>());
        Assert.NotNull(evt.PayloadJson);
        Assert.Contains("previousOrder", evt.PayloadJson);
        Assert.Contains("newOrder", evt.PayloadJson);
    }

    [Fact]
    public void ReorderPositions_NoChange_DoesNotRaiseDomainEvent()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Test");
        var pos1 = board.AddPosition(ControlNumber.Create(200), 1, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);
        var pos2 = board.AddPosition(ControlNumber.Create(201), 2, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);

        board.ReorderPositions([(pos1.CtrlNbr, 1), (pos2.CtrlNbr, 2)]);

        Assert.DoesNotContain(board.DomainEvents, e => e is PositionsReorderedDomainEvent);
    }

    }

    public class RosterBoardPositionTests
{
    [Fact]
    public void Hangout_SetsHungOutStatus()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Test");
        var pos = board.AddPosition(ControlNumber.Create(200), 1, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);

        pos.Hangout();

        Assert.Equal("HungOut", pos.HangoutStatus);
        Assert.NotNull(pos.HangoutAtUtc);
    }

    [Fact]
    public void MarkOff_SetsMarkedOffStatus()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Test");
        var pos = board.AddPosition(ControlNumber.Create(200), 1, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);

        pos.MarkOff();

        Assert.Equal("MarkedOff", pos.HangoutStatus);
    }

    [Fact]
    public void RestoreFromHangout_ResetsToActive()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Test");
        var pos = board.AddPosition(ControlNumber.Create(200), 1, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);
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
