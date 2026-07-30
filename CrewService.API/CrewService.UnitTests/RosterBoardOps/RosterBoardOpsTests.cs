using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.RosterBoardOps;

public class RosterBoardTests
{
    [Fact]
    public void Create_SetsActiveByDefault()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(2), ControlNumber.Create(100), "Yard Board");
        Assert.True(board.IsActive);
        Assert.Equal("Yard Board", board.Name);
    }

    [Fact]
    public void AddPosition_AddsToCollection()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(2), ControlNumber.Create(100), "Yard Board");
        board.AddPosition(ControlNumber.Create(10), 1, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);
        board.AddPosition(ControlNumber.Create(11), 2, StaffablePosition.Create(StaffablePositionType.Board).CtrlNbr);

        Assert.Equal(2, board.Positions.Count);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(2), ControlNumber.Create(100), "Yard Board");
        board.Deactivate();
        Assert.False(board.IsActive);
    }
}

public class RosterBoardPositionTests
{
    [Fact]
    public void AddPosition_SetsExpectedFields()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(2), ControlNumber.Create(100), "Test");
        var staffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
        var pos = board.AddPosition(ControlNumber.Create(10), 1, staffablePosition.CtrlNbr);

        Assert.Equal(board.CtrlNbr, pos.RosterBoardCtrlNbr);
        Assert.Equal(ControlNumber.Create(10), pos.EmployeeCtrlNbr);
        Assert.Equal(staffablePosition.CtrlNbr, pos.StaffablePositionCtrlNbr);
        Assert.Equal(1, pos.PositionOrder);
    }
}

public class DailyEmployeeStatusRecordTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var record = DailyEmployeeStatusRecord.Create(
            ControlNumber.Create(1), ControlNumber.Create(2),
            DateOnly.FromDateTime(DateTime.UtcNow), "OnDuty", "{\"shift\":\"AM\"}");

        Assert.Equal("OnDuty", record.StatusCode);
        Assert.NotNull(record.SnapshotJson);
    }
}
