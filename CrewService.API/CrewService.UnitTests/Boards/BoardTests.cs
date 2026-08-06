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

    [Theory]
    [InlineData(BoardType.ExtraBoard, true)]
    [InlineData(BoardType.Hangout, true)]
    [InlineData(BoardType.ExtendedAbsence, false)]
    [InlineData(BoardType.Training, false)]
    [InlineData(BoardType.NewHire, false)]
    public void Create_SetsAllowForceAssignDefaultByBoardType(BoardType boardType, bool expected)
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Board", boardType);

        Assert.Equal(expected, board.AllowForceAssign);
    }

    [Fact]
    public void SetAllowForceAssign_OverridesDefault()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Training", BoardType.Training);
        Assert.False(board.AllowForceAssign);

        board.SetAllowForceAssign(true);

        Assert.True(board.AllowForceAssign);
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

    [Theory]
    [InlineData(BoardType.ExtraBoard, false, false)]
    [InlineData(BoardType.Hangout, true, true)]
    [InlineData(BoardType.ExtendedAbsence, false, false)]
    [InlineData(BoardType.Training, false, false)]
    [InlineData(BoardType.NewHire, false, false)]
    public void Create_SetsPlacementNotificationDefaultsByBoardType(
        BoardType boardType,
        bool expectedNotifyOnPlacement,
        bool expectedPlacementRequiresAcknowledgement)
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Board", boardType);

        Assert.Equal(expectedNotifyOnPlacement, board.NotifyOnPlacement);
        Assert.Equal(expectedPlacementRequiresAcknowledgement, board.PlacementRequiresAcknowledgement);
    }

    [Fact]
    public void SetNotifyOnPlacement_False_ClearsPlacementRequiresAcknowledgement()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Hangout", BoardType.Hangout);
        Assert.True(board.NotifyOnPlacement);
        Assert.True(board.PlacementRequiresAcknowledgement);

        board.SetNotifyOnPlacement(false);

        Assert.False(board.NotifyOnPlacement);
        Assert.False(board.PlacementRequiresAcknowledgement);
    }

    [Fact]
    public void SetPlacementRequiresAcknowledgement_True_WhenNotifyDisabled_RemainsFalse()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Extra", BoardType.ExtraBoard);
        Assert.False(board.NotifyOnPlacement);

        board.SetPlacementRequiresAcknowledgement(true);

        Assert.False(board.PlacementRequiresAcknowledgement);
    }

    }

public class RosterBoardPositionTests
{
    [Fact]
    public void CreatePosition_SetsRequiredFields()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Test");
        var staffablePosition = StaffablePosition.Create(StaffablePositionType.Board);

        var pos = board.AddPosition(ControlNumber.Create(200), 1, staffablePosition.CtrlNbr);

        Assert.Equal(board.CtrlNbr, pos.RosterBoardCtrlNbr);
        Assert.Equal(ControlNumber.Create(200), pos.EmployeeCtrlNbr);
        Assert.Equal(staffablePosition.CtrlNbr, pos.StaffablePositionCtrlNbr);
        Assert.Equal(1, pos.PositionOrder);
        Assert.Equal(1, pos.OrderSeedBoardPosition);
        Assert.Null(pos.TieUpOrderUtc);
    }

    [Fact]
    public void Reorder_ByProtectedKeys_IsDeterministic()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Test");
        var staffable1 = StaffablePosition.Create(StaffablePositionType.Board);
        var staffable2 = StaffablePosition.Create(StaffablePositionType.Board);
        var staffable3 = StaffablePosition.Create(StaffablePositionType.Board);

        var pos1 = board.AddPosition(ControlNumber.Create(200), 1, staffable1.CtrlNbr);
        var pos2 = board.AddPosition(ControlNumber.Create(201), 2, staffable2.CtrlNbr);
        var pos3 = board.AddPosition(ControlNumber.Create(202), 3, staffable3.CtrlNbr);

        var t = new DateTime(2026, 8, 6, 22, 0, 0, DateTimeKind.Utc);
        pos1.SetTieUpOrderUtc(t);
        pos2.SetTieUpOrderUtc(t);
        pos3.SetTieUpOrderUtc(t.AddMinutes(30));

        pos1.SetOrderSeedBoardPosition(2);
        pos2.SetOrderSeedBoardPosition(1);
        pos3.SetOrderSeedBoardPosition(3);

        var ordering = board.Positions
            .OrderBy(p => p.TieUpOrderUtc ?? DateTime.MinValue)
            .ThenBy(p => p.OrderSeedBoardPosition)
            .ThenBy(p => p.PositionOrder)
            .ThenBy(p => p.CtrlNbr.Value)
            .Select((p, index) => (p.CtrlNbr, index + 1))
            .ToList();

        board.ReorderPositions(ordering);

        Assert.Equal(1, pos2.PositionOrder);
        Assert.Equal(2, pos1.PositionOrder);
        Assert.Equal(3, pos3.PositionOrder);
    }

    [Fact]
    public void SetTieUpOrderUtcIfLater_OnlyMovesForward()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(10), ControlNumber.Create(100), "Test");
        var pos = board.AddPosition(ControlNumber.Create(200), 1, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);

        var initial = new DateTime(2026, 8, 6, 20, 0, 0, DateTimeKind.Utc);
        var earlier = initial.AddMinutes(-10);
        var later = initial.AddMinutes(45);

        pos.SetTieUpOrderUtc(initial);
        pos.SetTieUpOrderUtcIfLater(earlier);
        Assert.Equal(initial, pos.TieUpOrderUtc);

        pos.SetTieUpOrderUtcIfLater(later);
        Assert.Equal(later, pos.TieUpOrderUtc);
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
