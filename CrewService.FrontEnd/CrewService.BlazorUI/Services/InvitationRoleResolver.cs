using System.Security.Claims;
using CrewService.BlazorUI.Client;
using CrewService.BlazorUI.Models.Auth;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Determines which roles the current user is allowed to assign when creating invitations.
/// Extracted from the Invitations component for testability.
/// </summary>
public static class InvitationRoleResolver
{
    /// <summary>
    /// Returns the roles available for the given user to assign, and the set of
    /// railroad CtrlNbrs the user is scoped to (empty for admins).
    /// </summary>
    public static (IReadOnlyList<string> AvailableRoles, HashSet<long> UserRailroadCtrlNbrs) Resolve(ClaimsPrincipal user)
    {
        var isSystemAdmin = user.IsInRole(Roles.SystemAdmin);
        var isParentAdmin = user.IsInRole(Roles.ParentAdmin)
                         || user.Claims.Any(c => c.Type == CustomClaimTypes.ParentRole
                             && c.Value.Split(':') is { Length: >= 2 } parts
                             && parts[1] == Roles.ParentAdmin);

        if (isSystemAdmin)
        {
            return ([.. Roles.SystemAdminAssignableRoles], []);
        }

        if (isParentAdmin)
        {
            return ([.. Roles.AdminAssignableRoles], []);
        }

        var userRailroadCtrlNbrs = user.Claims
            .Where(c => c.Type == CustomClaimTypes.ParentRole)
            .Select(c => c.Value.Split(':'))
            .Where(parts => parts.Length >= 3 && long.TryParse(parts[2], out _))
            .Select(parts => long.Parse(parts[2]))
            .ToHashSet();

        return ([.. Roles.OperationalRoles], userRailroadCtrlNbrs);
    }
}
