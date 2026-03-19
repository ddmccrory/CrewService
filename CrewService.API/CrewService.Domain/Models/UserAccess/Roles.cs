namespace CrewService.Domain.Models.UserAccess;

/// <summary>
/// Well-known role names for authorization. Per-parent roles are stored on
/// <see cref="UserParentAssignment.Role"/>. The global <see cref="SystemAdmin"/>
/// role is stored on <c>User.PrimaryRoleId</c> and bypasses parent scoping.
/// </summary>
public static class Roles
{
    // ?? Global (User.PrimaryRoleId) ??????????????????????????????????
    /// <summary>Full platform access across all parents.</summary>
    public const string SystemAdmin = "SystemAdmin";

    // ?? Per-Parent (UserParentAssignment.Role) ???????????????????????
    /// <summary>Full access within a parent, including user/role management.</summary>
    public const string ParentAdmin = "ParentAdmin";

    /// <summary>Full operational access within a parent; no user management.</summary>
    public const string RailroadAdmin = "RailroadAdmin";

    /// <summary>Employee management, seniority, rosters, displacement, craft policies.</summary>
    public const string CraftManager = "CraftManager";

    /// <summary>Crew staffing, bulletins, absence approvals.</summary>
    public const string CrewManager = "CrewManager";

    /// <summary>Dispatch operations, boards, mark-offs.</summary>
    public const string Dispatcher = "Dispatcher";

    /// <summary>Time entry and payroll processing.</summary>
    public const string PayrollClerk = "PayrollClerk";

    /// <summary>Standard employee access across all operational modules.</summary>
    public const string Employee = "Employee";

    /// <summary>All per-parent role names for validation.</summary>
    public static readonly IReadOnlyList<string> AllPerParentRoles =
    [
        ParentAdmin,
        RailroadAdmin,
        CraftManager,
        CrewManager,
        Dispatcher,
        PayrollClerk,
        Employee
    ];

    /// <summary>All roles that can be assigned via invitation (global + per-parent).</summary>
    public static readonly IReadOnlySet<string> AllInvitableRoles = new HashSet<string>
    {
        SystemAdmin,
        ParentAdmin,
        RailroadAdmin,
        CraftManager,
        CrewManager,
        Dispatcher,
        PayrollClerk,
        Employee
    };

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
