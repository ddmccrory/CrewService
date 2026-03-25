using System.Security.Claims;
using CrewService.BlazorUI.Client;
using CrewService.BlazorUI.Models.Auth;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Determines which roles the current user is allowed to assign when creating invitations.
/// Uses Level-based filtering so custom (non-system) roles are handled dynamically.
/// </summary>
public static class InvitationRoleResolver
{
    /// <summary>
    /// Returns the roles available for the given user to assign, and the set of
    /// railroad CtrlNbrs the user is scoped to (empty for admins).
    /// </summary>
    public static (IReadOnlyList<string> AvailableRoles, HashSet<long> UserRailroadCtrlNbrs) Resolve(
        ClaimsPrincipal user, IReadOnlyList<RoleResponse> allRoles)
    {
        var isSystemAdmin = user.IsInRole(Roles.SystemAdmin);
        var isParentAdmin = user.IsInRole(Roles.ParentAdmin)
                         || user.Claims.Any(c => c.Type == CustomClaimTypes.ParentRole
                             && c.Value.Split(':') is { Length: >= 2 } parts
                             && parts[1] == Roles.ParentAdmin);

        // Determine the user's highest role level from their claims
        var userRoleNames = ResolveUserRoleNames(user, allRoles);
        var userMaxLevel = allRoles
            .Where(r => userRoleNames.Contains(r.Name))
            .Select(r => r.Level)
            .DefaultIfEmpty(0)
            .Max();

        // User can assign any role at or below their level
        var assignableRoles = allRoles
            .Where(r => r.Level <= userMaxLevel)
            .OrderByDescending(r => r.Level)
            .Select(r => r.Name)
            .ToList();

        // Admin-tier users see all railroads; others are scoped
        if (isSystemAdmin || isParentAdmin)
            return (assignableRoles, []);

        var userRailroadCtrlNbrs = user.Claims
            .Where(c => c.Type == CustomClaimTypes.ParentRole)
            .Select(c => c.Value.Split(':'))
            .Where(parts => parts.Length >= 3 && long.TryParse(parts[2], out _))
            .Select(parts => long.Parse(parts[2]))
            .ToHashSet();

        return (assignableRoles, userRailroadCtrlNbrs);
    }

    private static HashSet<string> ResolveUserRoleNames(ClaimsPrincipal user, IReadOnlyList<RoleResponse> allRoles)
    {
        var names = new HashSet<string>();
        foreach (var role in allRoles)
        {
            if (user.IsInRole(role.Name))
                names.Add(role.Name);
        }
        foreach (var claim in user.Claims.Where(c => c.Type == CustomClaimTypes.ParentRole))
        {
            var parts = claim.Value.Split(':');
            if (parts.Length >= 2)
                names.Add(parts[1]);
        }
        return names;
    }
}
