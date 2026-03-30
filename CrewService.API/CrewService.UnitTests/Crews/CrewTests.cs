using CrewService.Domain.Modules.Crews;
using Xunit;

namespace CrewService.UnitTests.Crews;

public class CrewTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var crew = Crew.Create("REGULAR", 1, "Crew Alpha");

        Assert.Equal("REGULAR", crew.CrewType);
        Assert.Equal("Crew Alpha", crew.Name);
        Assert.True(crew.IsActive);
        Assert.True(crew.DomainEvents.Count > 0);
    }

    [Fact]
    public void Update_ChangesNameAndActiveStatus()
    {
        var crew = Crew.Create("REGULAR", 1, "Old Name");

        crew.Update("New Name", false);

        Assert.Equal("New Name", crew.Name);
        Assert.False(crew.IsActive);
    }
}

public class CrewPositionTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var position = CrewPosition.Create(1, 10, 2);

        Assert.Equal(1, position.CrewCtrlNbr.Value);
        Assert.Equal(10, position.CraftRoleCtrlNbr.Value);
        Assert.Equal(2, position.DisplayOrder);
    }
}

public class CrewIncumbencyTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var start = DateTime.UtcNow;
        var incumbency = CrewIncumbency.Create(1, 100, start);

        Assert.Equal(1, incumbency.CrewPositionCtrlNbr.Value);
        Assert.Equal(100, incumbency.EmployeeCtrlNbr.Value);
        Assert.Equal(start, incumbency.StartUtc);
        Assert.Null(incumbency.EndUtc);
    }

    [Fact]
    public void Create_WithEndUtc_SetsEndUtc()
    {
        var start = DateTime.UtcNow;
        var end = start.AddDays(30);
        var incumbency = CrewIncumbency.Create(1, 100, start, end);

        Assert.Equal(end, incumbency.EndUtc);
    }
}

public class CrewAssignmentTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var start = DateTime.UtcNow;
        var assignment = CrewAssignment.Create(10, 20, 0b0111110, start);

        Assert.Equal(10, assignment.CrewCtrlNbr.Value);
        Assert.Equal(20, assignment.AssignmentGroupCtrlNbr.Value);
        Assert.Equal(0b0111110, assignment.DaysOfWeekMask);
        Assert.Equal(start, assignment.StartUtc);
        Assert.Null(assignment.EndUtc);
    }

    [Fact]
    public void Create_WithEndUtc_SetsEndUtc()
    {
        var start = DateTime.UtcNow;
        var end = start.AddDays(7);
        var assignment = CrewAssignment.Create(10, 20, 0b0111110, start, end);

        Assert.Equal(end, assignment.EndUtc);
    }

    [Fact]
    public void Update_ChangesProperties()
    {
        var start = DateTime.UtcNow;
        var assignment = CrewAssignment.Create(10, 20, 0b0111110, start);
        var newStart = start.AddDays(1);
        var newEnd = start.AddDays(30);

        assignment.Update(0b1111111, newStart, newEnd);

        Assert.Equal(0b1111111, assignment.DaysOfWeekMask);
        Assert.Equal(newStart, assignment.StartUtc);
        Assert.Equal(newEnd, assignment.EndUtc);
    }

    [Fact]
    public void Create_WithZeroMask_MeansNoDays()
    {
        var assignment = CrewAssignment.Create(10, 20, 0, DateTime.UtcNow);

        Assert.Equal(0, assignment.DaysOfWeekMask);
    }
}