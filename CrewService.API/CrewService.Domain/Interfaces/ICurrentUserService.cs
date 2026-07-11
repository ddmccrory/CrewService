namespace CrewService.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid GetUserId();
    string GetUserName();
    bool IsInRole(string roleName);

    /// <summary>
    /// Returns the CtrlNbr of the parent the caller is currently operating under,
    /// or <c>null</c> when no parent context is available (e.g. system-level operations).
    /// </summary>
    long? GetParentCtrlNbr();

    /// <summary>
    /// Sets an audit-name override for unauthenticated operations (e.g. registration).
    /// Scoped per request; takes precedence over claims-based resolution.
    /// </summary>
    void SetAuditOverride(string name);
}
