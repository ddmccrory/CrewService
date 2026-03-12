using CrewService.Domain.Modules.Boards;
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
            ControlNumber.Create(1), ControlNumber.Create(2), "Yard Board");
        Assert.True(board.IsActive);
        Assert.Equal("Yard Board", board.Name);
    }

    [Fact]
    public void AddPosition_AddsToCollection()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(1), ControlNumber.Create(2), "Yard Board");
        board.AddPosition(ControlNumber.Create(10), 1);
        board.AddPosition(ControlNumber.Create(11), 2);

        Assert.Equal(2, board.Positions.Count);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(1), ControlNumber.Create(2), "Yard Board");
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
            ControlNumber.Create(1), ControlNumber.Create(2), "Test");
        var pos = board.AddPosition(ControlNumber.Create(10), 1);
        pos.Hangout();

        Assert.Equal("HungOut", pos.HangoutStatus);
        Assert.NotNull(pos.HangoutAtUtc);
    }

    [Fact]
    public void MarkOff_SetsMarkedOffStatus()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(1), ControlNumber.Create(2), "Test");
        var pos = board.AddPosition(ControlNumber.Create(10), 1);
        pos.MarkOff();

        Assert.Equal("MarkedOff", pos.HangoutStatus);
    }

    [Fact]
    public void Restore_ResetsToActive()
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(1), ControlNumber.Create(2), "Test");
        var pos = board.AddPosition(ControlNumber.Create(10), 1);
        pos.Hangout();
        pos.Restore();

        Assert.Equal("Active", pos.HangoutStatus);
        Assert.Null(pos.HangoutAtUtc);
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
