using CrewService.BlazorUI.Models.Auth;
using Xunit;

namespace CrewService.BlazorUI.Tests.Models.Auth;

public class RolesTests
{
    [Fact]
    public void RequiresRailroad_ReturnsFalse_ForSystemAdmin()
    {
        Assert.False(Roles.RequiresRailroad(Roles.SystemAdmin));
    }

    [Fact]
    public void RequiresRailroad_ReturnsFalse_ForParentAdmin()
    {
        Assert.False(Roles.RequiresRailroad(Roles.ParentAdmin));
    }

    [Theory]
    [InlineData(Roles.RailroadAdmin)]
    [InlineData(Roles.Employee)]
    [InlineData("CraftManager")]
    [InlineData("CustomRole")]
    public void RequiresRailroad_ReturnsTrue_ForNonParentScopedRoles(string role)
    {
        Assert.True(Roles.RequiresRailroad(role));
    }
}
