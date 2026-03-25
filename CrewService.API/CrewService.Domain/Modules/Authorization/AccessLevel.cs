namespace CrewService.Domain.Modules.Authorization;

/// <summary>
/// Defines the level of access a role has to a feature.
/// </summary>
public enum AccessLevel
{
    /// <summary>No access - menu item hidden, endpoint blocked.</summary>
    None = 0,

    /// <summary>Can view but not create, edit, or delete.</summary>
    ReadOnly = 1,

    /// <summary>Full CRUD access.</summary>
    FullAccess = 2
}
