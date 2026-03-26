namespace CrewService.Domain.Models.UserAccess;

/// <summary>
/// Well-known system-protected role names for authorization.
/// Only roles that have hardcoded behavioral logic are defined here.
/// Operational roles are data-driven and managed through the UI.
/// </summary>
public static class Roles
{
    /// <summary>Full platform access across all parents.</summary>
    public const string SystemAdmin = "SystemAdmin";

    /// <summary>Full access within a parent, including user/role management.</summary>
    public const string ParentAdmin = "ParentAdmin";

    /// <summary>Full operational access within a parent; no user management.</summary>
    public const string RailroadAdmin = "RailroadAdmin";

    /// <summary>Standard employee access across all operational modules.</summary>
    public const string Employee = "Employee";

    /// <summary>
    /// Returns <c>true</c> if the given role requires a railroad selection.
    /// Only SystemAdmin and ParentAdmin are parent-scoped; all others require a railroad.
    /// </summary>
    public static bool RequiresRailroad(string roleName) =>
        roleName != SystemAdmin && roleName != ParentAdmin;
}
