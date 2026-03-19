namespace CrewService.Domain.Constants;

/// <summary>
/// Custom JWT claim type names used in the CrewService auth contract.
/// Centralises the strings shared between token creation and token consumption.
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
