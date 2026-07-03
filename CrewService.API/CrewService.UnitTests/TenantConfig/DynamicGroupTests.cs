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

    [Fact]
    public void OwningRailroadCtrlNbr_WhenWorkAreaReferencesRailroad_ReturnsRailroadCtrlNbr()
    {
        // Standard topology: a work-area group points at a separate railroad group.
        var railroadCtrlNbr = ControlNumber.Create(500);
        var workArea = DynamicGroup.Create(
            1, "Houston Yard", parentGroupCtrlNbr: null, path: null,
            isWorkArea: true, railroadCtrlNbr: railroadCtrlNbr);

        Assert.Equal(railroadCtrlNbr, workArea.OwningRailroadCtrlNbr);
    }

    [Fact]
    public void OwningRailroadCtrlNbr_WhenRailroadIsItsOwnWorkArea_ReturnsOwnCtrlNbr()
    {
        // Small-railroad topology (e.g. PTRA): the railroad group IS the work area, so
        // RailroadCtrlNbr is null and the group's own CtrlNbr is the owning railroad.
        var group = DynamicGroup.Create(
            1, "PTRA", parentGroupCtrlNbr: null, path: null,
            isWorkArea: true, railroadCtrlNbr: null);

        Assert.Null(group.RailroadCtrlNbr);
        Assert.Equal(group.CtrlNbr, group.OwningRailroadCtrlNbr);
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

public class TeamsWebhookConfigTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var config = TeamsWebhookConfig.Create(
            ControlNumber.Create(1), ControlNumber.Create(10),
            Domain.Interfaces.NotificationChannel.ElectronicCall,
            "https://webhook.example.com", true);

        Assert.Equal("https://webhook.example.com", config.WebhookUrl);
        Assert.True(config.IsEnabled);
    }

    [Fact]
    public void Update_ChangesWebhookAndEnabled()
    {
        var config = TeamsWebhookConfig.Create(
            ControlNumber.Create(1), null,
            Domain.Interfaces.NotificationChannel.ElectronicCall,
            "https://old.example.com", true);

        config.Update("https://new.example.com", false, "admin");

        Assert.Equal("https://new.example.com", config.WebhookUrl);
        Assert.False(config.IsEnabled);
    }
}
