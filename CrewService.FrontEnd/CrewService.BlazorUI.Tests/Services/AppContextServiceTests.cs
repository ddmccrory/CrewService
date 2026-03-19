using CrewService.BlazorUI.Services;
using Xunit;

namespace CrewService.BlazorUI.Tests.Services;

public class AppContextServiceTests
{
    [Fact]
    public void InitialState_HasNoSelections()
    {
        var svc = new AppContextService();

        Assert.False(svc.HasParent);
        Assert.False(svc.HasRailroad);
        Assert.False(svc.IsFullySelected);
        Assert.Null(svc.SelectedParentCtrlNbr);
        Assert.Null(svc.SelectedParentName);
        Assert.Null(svc.SelectedRailroadCtrlNbr);
        Assert.Null(svc.SelectedRailroadName);
    }

    [Fact]
    public void SetParent_SetsParentProperties()
    {
        var svc = new AppContextService();

        svc.SetParent(42, "BNSF");

        Assert.True(svc.HasParent);
        Assert.Equal(42, svc.SelectedParentCtrlNbr);
        Assert.Equal("BNSF", svc.SelectedParentName);
    }

    [Fact]
    public void SetParent_ClearsRailroad()
    {
        var svc = new AppContextService();
        svc.SetParent(1, "Parent");
        svc.SetRailroad(10, "Railroad");

        svc.SetParent(2, "New Parent");

        Assert.True(svc.HasParent);
        Assert.False(svc.HasRailroad);
        Assert.Null(svc.SelectedRailroadCtrlNbr);
        Assert.Null(svc.SelectedRailroadName);
    }

    [Fact]
    public void SetRailroad_WithParent_SetsRailroadProperties()
    {
        var svc = new AppContextService();
        svc.SetParent(1, "Parent");

        svc.SetRailroad(10, "UP");

        Assert.True(svc.HasRailroad);
        Assert.True(svc.IsFullySelected);
        Assert.Equal(10, svc.SelectedRailroadCtrlNbr);
        Assert.Equal("UP", svc.SelectedRailroadName);
    }

    [Fact]
    public void SetRailroad_WithoutParent_IsIgnored()
    {
        var svc = new AppContextService();

        svc.SetRailroad(10, "UP");

        Assert.False(svc.HasRailroad);
        Assert.Null(svc.SelectedRailroadCtrlNbr);
    }

    [Fact]
    public void ClearRailroad_KeepsParent()
    {
        var svc = new AppContextService();
        svc.SetParent(1, "Parent");
        svc.SetRailroad(10, "Railroad");

        svc.ClearRailroad();

        Assert.True(svc.HasParent);
        Assert.False(svc.HasRailroad);
        Assert.False(svc.IsFullySelected);
    }

    [Fact]
    public void Clear_ResetsEverything()
    {
        var svc = new AppContextService();
        svc.SetParent(1, "Parent");
        svc.SetRailroad(10, "Railroad");

        svc.Clear();

        Assert.False(svc.HasParent);
        Assert.False(svc.HasRailroad);
        Assert.False(svc.IsFullySelected);
        Assert.Null(svc.SelectedParentCtrlNbr);
        Assert.Null(svc.SelectedRailroadCtrlNbr);
    }

    [Fact]
    public void SetParent_FiresOnContextChanged()
    {
        var svc = new AppContextService();
        var fired = false;
        svc.OnContextChanged += () => fired = true;

        svc.SetParent(1, "Parent");

        Assert.True(fired);
    }

    [Fact]
    public void SetRailroad_WithParent_FiresOnContextChanged()
    {
        var svc = new AppContextService();
        svc.SetParent(1, "Parent");
        var fired = false;
        svc.OnContextChanged += () => fired = true;

        svc.SetRailroad(10, "Railroad");

        Assert.True(fired);
    }

    [Fact]
    public void SetRailroad_WithoutParent_DoesNotFireOnContextChanged()
    {
        var svc = new AppContextService();
        var fired = false;
        svc.OnContextChanged += () => fired = true;

        svc.SetRailroad(10, "Railroad");

        Assert.False(fired);
    }

    [Fact]
    public void ClearRailroad_FiresOnContextChanged()
    {
        var svc = new AppContextService();
        svc.SetParent(1, "Parent");
        svc.SetRailroad(10, "Railroad");
        var fired = false;
        svc.OnContextChanged += () => fired = true;

        svc.ClearRailroad();

        Assert.True(fired);
    }

    [Fact]
    public void Clear_FiresOnContextChanged()
    {
        var svc = new AppContextService();
        svc.SetParent(1, "Parent");
        var fired = false;
        svc.OnContextChanged += () => fired = true;

        svc.Clear();

        Assert.True(fired);
    }
}
