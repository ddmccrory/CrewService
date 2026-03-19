using System.Security.Claims;
using CrewService.BlazorUI.Client;
using CrewService.BlazorUI.Models.Auth;
using CrewService.BlazorUI.Services;
using Xunit;

namespace CrewService.BlazorUI.Tests.Services;

public class InvitationRoleResolverTests
{
    private static ClaimsPrincipal CreateUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    // ── SystemAdmin ──────────────────────────────────────────────────

    [Fact]
    public void SystemAdmin_GetsSystemAdminAssignableRoles()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.SystemAdmin));

        var (roles, _) = InvitationRoleResolver.Resolve(user);

        Assert.Equal(Roles.SystemAdminAssignableRoles, roles);
    }

    [Fact]
    public void SystemAdmin_IncludesSystemAdminInRoles()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.SystemAdmin));

        var (roles, _) = InvitationRoleResolver.Resolve(user);

        Assert.Contains(Roles.SystemAdmin, roles);
    }

    [Fact]
    public void SystemAdmin_ReturnsEmptyRailroads()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.SystemAdmin));

        var (_, railroads) = InvitationRoleResolver.Resolve(user);

        Assert.Empty(railroads);
    }

    // ── ParentAdmin (via identity role) ──────────────────────────────

    [Fact]
    public void ParentAdmin_ViaIdentityRole_GetsAdminAssignableRoles()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.ParentAdmin));

        var (roles, _) = InvitationRoleResolver.Resolve(user);

        Assert.Equal(Roles.AdminAssignableRoles, roles);
    }

    [Fact]
    public void ParentAdmin_DoesNotIncludeSystemAdmin()
    {
        var user = CreateUser(new Claim(ClaimTypes.Role, Roles.ParentAdmin));

        var (roles, _) = InvitationRoleResolver.Resolve(user);

        Assert.DoesNotContain(Roles.SystemAdmin, roles);
    }

    // ── ParentAdmin (via parent_role claim) ──────────────────────────

    [Fact]
    public void ParentAdmin_ViaParentRoleClaim_GetsAdminAssignableRoles()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.ParentAdmin}"));

        var (roles, _) = InvitationRoleResolver.Resolve(user);

        Assert.Equal(Roles.AdminAssignableRoles, roles);
    }

    [Fact]
    public void ParentAdmin_ViaParentRoleClaimWithRailroad_GetsAdminAssignableRoles()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.ParentAdmin}:200"));

        var (roles, _) = InvitationRoleResolver.Resolve(user);

        Assert.Equal(Roles.AdminAssignableRoles, roles);
    }

    // ── RailroadAdmin / operational ─────────────────────────────────

    [Fact]
    public void RailroadAdmin_GetsOperationalRoles()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.RailroadAdmin}:200"));

        var (roles, _) = InvitationRoleResolver.Resolve(user);

        Assert.Equal(Roles.OperationalRoles, roles);
    }

    [Fact]
    public void RailroadAdmin_DoesNotIncludeAdminRoles()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.RailroadAdmin}:200"));

        var (roles, _) = InvitationRoleResolver.Resolve(user);

        Assert.DoesNotContain(Roles.SystemAdmin, roles);
        Assert.DoesNotContain(Roles.ParentAdmin, roles);
        Assert.DoesNotContain(Roles.RailroadAdmin, roles);
    }

    [Fact]
    public void RailroadAdmin_ExtractsRailroadCtrlNbrs()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.RailroadAdmin}:200"),
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.RailroadAdmin}:300"));

        var (_, railroads) = InvitationRoleResolver.Resolve(user);

        Assert.Equal(new HashSet<long> { 200, 300 }, railroads);
    }

    [Fact]
    public void OperationalUser_WithSingleRailroad_ReturnsCorrectSet()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.Dispatcher}:500"));

        var (roles, railroads) = InvitationRoleResolver.Resolve(user);

        Assert.Equal(Roles.OperationalRoles, roles);
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

        var (roles, _) = InvitationRoleResolver.Resolve(user);

        Assert.Contains(Roles.SystemAdmin, roles);
        Assert.Equal(Roles.SystemAdminAssignableRoles, roles);
    }

    [Fact]
    public void UserWithNoRoles_GetsOperationalRoles()
    {
        var user = CreateUser();

        var (roles, railroads) = InvitationRoleResolver.Resolve(user);

        Assert.Equal(Roles.OperationalRoles, roles);
        Assert.Empty(railroads);
    }

    [Fact]
    public void ClaimWithoutRailroadSegment_DoesNotExtractRailroad()
    {
        var user = CreateUser(
            new Claim(CustomClaimTypes.ParentRole, $"100:{Roles.CraftManager}"));

        var (_, railroads) = InvitationRoleResolver.Resolve(user);

        Assert.Empty(railroads);
    }
}
