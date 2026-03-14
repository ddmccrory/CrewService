using CrewService.Domain.Modules.Boards;
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
