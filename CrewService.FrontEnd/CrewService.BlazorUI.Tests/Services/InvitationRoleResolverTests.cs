using System.Security.Claims;
using CrewService.BlazorUI.Client;
using CrewService.BlazorUI.Models.Auth;
using CrewService.BlazorUI.Services;
using CrewService.Presentation;
using Xunit;

namespace CrewService.BlazorUI.Tests.Services;

public class InvitationRoleResolverTests
{
    private static readonly IReadOnlyList<RoleResponse> TestRoles =
    [
        new() { Name = "SystemAdmin", Level = 100, IsSystem = true },
        new() { Name = "ParentAdmin", Level = 80, IsSystem = true },
        new() { Name = "RailroadAdmin", Level = 60, IsSystem = true },
        new() { Name = "CraftManager", Level = 40, IsSystem = false },
        new() { Name = "CrewManager", Level = 40, IsSystem = false },
        new() { Name = "Dispatcher", Level = 40, IsSystem = false },
        new() { Name = "PayrollClerk", Level = 40, IsSystem = false },
        new() { Name = "Employee", Level = 20, IsSystem = true },
    ];

    private static ClaimsPrincipal CreateUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    // ── SystemAdmin ──────────────────────────────────────────────────

    [Fact]
    public void SystemAdmin_GetsAllRoles()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.SystemAdmin));

        var (roles, _) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.Equal(TestRoles.Count, roles.Count);
    }

    [Fact]
    public void SystemAdmin_IncludesSystemAdminInRoles()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.SystemAdmin));

        var (roles, _) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.Contains(Roles.SystemAdmin, roles);
    }

    [Fact]
    public void SystemAdmin_ReturnsEmptyRailroads()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.SystemAdmin));

        var (_, railroads) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.Empty(railroads);
    }

    [Fact]
    public void SystemAdmin_RolesOrderedByLevelDescending()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.SystemAdmin));

        var (roles, _) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.Equal(Roles.SystemAdmin, roles[0]);
        Assert.Equal(Roles.Employee, roles[^1]);
    }

    // ── ParentAdmin (via identity role) ──────────────────────────────

    [Fact]
    public void ParentAdmin_ViaIdentityRole_GetsRolesAtOrBelowLevel()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.ParentAdmin));

        var (roles, _) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.DoesNotContain(Roles.SystemAdmin, roles);
        Assert.Contains(Roles.ParentAdmin, roles);
        Assert.Contains(Roles.RailroadAdmin, roles);
        Assert.Contains(Roles.Employee, roles);
    }

    [Fact]
    public void ParentAdmin_ReturnsEmptyRailroads()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.ParentAdmin));

        var (_, railroads) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.Empty(railroads);
    }

    // ── ParentAdmin (via parent_role claim) ──────────────────────────

    [Fact]
    public void ParentAdmin_ViaParentRoleClaim_GetsRolesAtOrBelowLevel()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.ParentAdmin}"));

        var (roles, _) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.DoesNotContain(Roles.SystemAdmin, roles);
        Assert.Contains(Roles.ParentAdmin, roles);
    }

    [Fact]
    public void ParentAdmin_ViaParentRoleClaimWithRailroad_GetsRolesAtOrBelowLevel()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.ParentAdmin}:200"));

        var (roles, _) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.DoesNotContain(Roles.SystemAdmin, roles);
        Assert.Contains(Roles.ParentAdmin, roles);
    }

    // ── RailroadAdmin / operational ─────────────────────────────────

    [Fact]
    public void RailroadAdmin_GetsRolesAtOrBelowLevel()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.RailroadAdmin}:200"));

        var (roles, _) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.DoesNotContain(Roles.SystemAdmin, roles);
        Assert.DoesNotContain(Roles.ParentAdmin, roles);
        Assert.Contains(Roles.RailroadAdmin, roles);
        Assert.Contains("CraftManager", roles);
        Assert.Contains(Roles.Employee, roles);
    }

    [Fact]
    public void RailroadAdmin_ExtractsRailroadCtrlNbrs()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.RailroadAdmin}:200"),
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.RailroadAdmin}:300"));

        var (_, railroads) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.Equal(new HashSet<long> { 200, 300 }, railroads);
    }

    [Fact]
    public void OperationalUser_WithSingleRailroad_ReturnsCorrectSet()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:Dispatcher:500"));

        var (roles, railroads) = InvitationRoleResolver.Resolve(user, TestRoles);

        // Dispatcher is Level 40 — can assign Level <= 40
        Assert.Contains("Dispatcher", roles);
        Assert.Contains(Roles.Employee, roles);
        Assert.DoesNotContain(Roles.RailroadAdmin, roles);
        Assert.Single(railroads);
        Assert.Contains(500, railroads);
    }

    // ── Edge cases ──────────────────────────────────────────────────

    [Fact]
    public void SystemAdmin_TakesPrecedenceOverParentAdmin()
    {
        var user = CreateUser(
            new Claim(ClaimTypes.Role, Roles.SystemAdmin),
            new Claim(ClaimTypes.Role, Roles.ParentAdmin));

        var (roles, _) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.Contains(Roles.SystemAdmin, roles);
        Assert.Equal(TestRoles.Count, roles.Count);
    }

    [Fact]
    public void UserWithNoRoles_GetsNoAssignableRoles()
    {
        var user = CreateUser();

        var (roles, railroads) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.Empty(roles);
        Assert.Empty(railroads);
    }

    [Fact]
    public void ClaimWithoutRailroadSegment_DoesNotExtractRailroad()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:CraftManager"));

        var (_, railroads) = InvitationRoleResolver.Resolve(user, TestRoles);

        Assert.Empty(railroads);
    }

    [Fact]
    public void CustomRole_IncludedInAssignableRoles()
    {
        var rolesWithCustom = new List<RoleResponse>(TestRoles)
        {
            new() { Name = "CustomRole", Level = 50, IsSystem = false }
        };

        var user = CreateUser(
            new Claim(ClaimTypes.Role, Roles.ParentAdmin));

        var (roles, _) = InvitationRoleResolver.Resolve(user, rolesWithCustom);

        Assert.Contains("CustomRole", roles);
    }
}
