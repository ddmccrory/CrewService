namespace CrewService.BlazorUI.Client;

/// <summary>
/// Custom JWT claim type names used in the CrewService auth contract.
/// Mirrors the server-side constants to keep magic strings out of WASM client logic.
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>Compound claim: "{parentCtrlNbr}:{role}" or "{parentCtrlNbr}:{role}:{railroadCtrlNbr}".</summary>
    public const string ParentRole = "parent_role";

    /// <summary>Unique GUID per login session, used to scope ProtectedSessionStorage.</summary>
    public const string LoginId = "login_id";

    /// <summary>Employee number from the user profile, when available.</summary>
    public const string EmployeeNumber = "employee_number";
}
