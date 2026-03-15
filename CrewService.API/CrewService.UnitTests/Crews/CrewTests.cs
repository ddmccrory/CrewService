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
        Assert.Equal(10, position.PositionRoleCtrlNbr.Value);
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

public class CrewAttachmentTemplateTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var start = DateTime.UtcNow;
        var attachment = CrewAttachmentTemplate.Create(10, 20, start);

        Assert.Equal(10, attachment.AssignmentTemplateCtrlNbr.Value);
        Assert.Equal(20, attachment.CrewCtrlNbr.Value);
        Assert.Equal(start, attachment.StartUtc);
        Assert.Null(attachment.EndUtc);
    }

    [Fact]
    public void Create_WithEndUtc_SetsEndUtc()
    {
        var start = DateTime.UtcNow;
        var end = start.AddDays(7);
        var attachment = CrewAttachmentTemplate.Create(10, 20, start, end);

        Assert.Equal(end, attachment.EndUtc);
    }
}

public class ReliefCoverageRuleTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var start = DateTime.UtcNow;
        var rule = ReliefCoverageRule.Create(1, 2, 0b1111100, start);

        Assert.Equal(1, rule.ReliefCrewCtrlNbr.Value);
        Assert.Equal(2, rule.AssignmentTemplateCtrlNbr.Value);
        Assert.Equal(0b1111100, rule.DaysOfWeekMask);
        Assert.Equal(start, rule.StartUtc);
        Assert.Null(rule.EndUtc);
    }
}
