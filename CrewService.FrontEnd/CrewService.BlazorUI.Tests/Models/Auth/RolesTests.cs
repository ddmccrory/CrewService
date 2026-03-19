using CrewService.BlazorUI.Models.Auth;
using Xunit;

namespace CrewService.BlazorUI.Tests.Models.Auth;

public class RolesTests
{
    [Fact]
    public void AdminAssignableRoles_DoesNotContainSystemAdmin()
    {
        Assert.DoesNotContain(Roles.SystemAdmin, Roles.AdminAssignableRoles);
    }

    [Fact]
    public void SystemAdminAssignableRoles_ContainsSystemAdmin()
    {
        Assert.Contains(Roles.SystemAdmin, Roles.SystemAdminAssignableRoles);
    }

    [Fact]
    public void SystemAdminAssignableRoles_ContainsAllAdminAssignableRoles()
    {
        foreach (var role in Roles.AdminAssignableRoles)
        {
            Assert.Contains(role, Roles.SystemAdminAssignableRoles);
        }
    }

    [Fact]
    public void SystemAdminAssignableRoles_HasSystemAdminFirst()
    {
        Assert.Equal(Roles.SystemAdmin, Roles.SystemAdminAssignableRoles[0]);
    }

    [Fact]
    public void SystemAdminAssignableRoles_CountIsAdminAssignableRolesPlusOne()
    {
        Assert.Equal(Roles.AdminAssignableRoles.Count + 1, Roles.SystemAdminAssignableRoles.Count);
    }

    [Fact]
    public void OperationalRoles_IsSubsetOfAdminAssignableRoles()
    {
        foreach (var role in Roles.OperationalRoles)
        {
            Assert.Contains(role, Roles.AdminAssignableRoles);
        }
    }

    [Fact]
    public void OperationalRoles_DoesNotContainAdminRoles()
    {
        Assert.DoesNotContain(Roles.SystemAdmin, Roles.OperationalRoles);
        Assert.DoesNotContain(Roles.ParentAdmin, Roles.OperationalRoles);
        Assert.DoesNotContain(Roles.RailroadAdmin, Roles.OperationalRoles);
    }

    [Fact]
    public void RolesRequiringRailroad_DoesNotContainSystemAdmin()
    {
        Assert.DoesNotContain(Roles.SystemAdmin, Roles.RolesRequiringRailroad);
    }

    [Fact]
    public void RolesRequiringRailroad_DoesNotContainParentAdmin()
    {
        Assert.DoesNotContain(Roles.ParentAdmin, Roles.RolesRequiringRailroad);
    }

    [Theory]
    [InlineData(Roles.RailroadAdmin)]
    [InlineData(Roles.CraftManager)]
    [InlineData(Roles.CrewManager)]
    [InlineData(Roles.Dispatcher)]
    [InlineData(Roles.PayrollClerk)]
    [InlineData(Roles.Employee)]
    public void RolesRequiringRailroad_ContainsExpectedRole(string role)
    {
        Assert.Contains(role, Roles.RolesRequiringRailroad);
    }
}
