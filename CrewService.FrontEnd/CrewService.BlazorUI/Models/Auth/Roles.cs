namespace CrewService.BlazorUI.Models.Auth;

/// <summary>
/// Well-known role name constants mirroring the API domain.
/// Keeps magic strings out of Razor components and client-side logic.
/// </summary>
public static class Roles
{
    // Global
    public const string SystemAdmin = "SystemAdmin";

    // Per-Parent
    public const string ParentAdmin = "ParentAdmin";
    public const string RailroadAdmin = "RailroadAdmin";
    public const string CraftManager = "CraftManager";
    public const string CrewManager = "CrewManager";
    public const string Dispatcher = "Dispatcher";
    public const string PayrollClerk = "PayrollClerk";
    public const string Employee = "Employee";

    /// <summary>All per-parent role names (admin + operational).</summary>
    public static readonly IReadOnlyList<string> AdminAssignableRoles =
    [
        ParentAdmin,
        RailroadAdmin,
        CraftManager,
        CrewManager,
        Dispatcher,
        PayrollClerk,
        Employee
    ];

    /// <summary>Operational roles assignable by non-parent-admin users.</summary>
    public static readonly IReadOnlyList<string> OperationalRoles =
    [
        CraftManager,
        CrewManager,
        Dispatcher,
        PayrollClerk,
        Employee
    ];

    /// <summary>Roles that require a railroad selection.</summary>
    public static readonly IReadOnlySet<string> RolesRequiringRailroad = new HashSet<string>
    {
        RailroadAdmin,
        CraftManager,
        CrewManager,
        Dispatcher,
        PayrollClerk,
        Employee
    };
}
