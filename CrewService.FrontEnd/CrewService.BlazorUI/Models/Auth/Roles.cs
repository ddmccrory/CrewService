namespace CrewService.BlazorUI.Models.Auth;

/// <summary>
/// Well-known system-protected role name constants.
/// Only roles that have hardcoded behavioral logic are defined here.
/// Operational roles (CraftManager, CrewManager, etc.) are data-driven
/// and managed through the Roles page.
/// </summary>
public static class Roles
{
    // System-protected roles
    public const string SystemAdmin = "SystemAdmin";
    public const string ParentAdmin = "ParentAdmin";
    public const string RailroadAdmin = "RailroadAdmin";
    public const string Employee = "Employee";

    /// <summary>
    /// Returns <c>true</c> if the given role requires a railroad selection during invitation.
    /// Only SystemAdmin and ParentAdmin are parent-scoped; all others require a railroad.
    /// </summary>
    public static bool RequiresRailroad(string roleName) =>
        roleName != SystemAdmin && roleName != ParentAdmin;
}
