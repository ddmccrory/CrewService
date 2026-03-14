using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.TenantConfig;

public class DynamicGroupTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var group = DynamicGroup.Create(1, "North Division", null, "/North", true);

        Assert.Equal("North Division", group.Name);
        Assert.Null(group.ParentGroupCtrlNbr);
        Assert.Equal("/North", group.Path);
        Assert.True(group.IsWorkArea);
        Assert.True(group.DomainEvents.Count > 0);
    }

    [Fact]
    public void Create_WithParent_SetsParentCtrlNbr()
    {
        var group = DynamicGroup.Create(1, "Sub Group", 100, "/North/Sub", false);

        Assert.Equal(100, group.ParentGroupCtrlNbr!.Value);
        Assert.False(group.IsWorkArea);
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var group = DynamicGroup.Create(1, "Old Name", null, "/Old", false);

        group.Update("New Name", ControlNumber.Create(200), "/New", true);

        Assert.Equal("New Name", group.Name);
        Assert.Equal(200, group.ParentGroupCtrlNbr!.Value);
        Assert.Equal("/New", group.Path);
        Assert.True(group.IsWorkArea);
    }
}

public class GroupTypeTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var gt = GroupType.Create("Railroad", "Railroad group type", true);

        Assert.Equal("Railroad", gt.Name);
        Assert.Equal("Railroad group type", gt.Description);
        Assert.True(gt.IsWorkArea);
        Assert.True(gt.DomainEvents.Count > 0);
    }

    [Fact]
    public void Update_ChangesProperties()
    {
        var gt = GroupType.Create("Old", "Desc", false);

        gt.Update("New", "New Desc", true, null);

        Assert.Equal("New", gt.Name);
        Assert.True(gt.IsWorkArea);
    }
}
