namespace CrewService.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid GetUserId();
    string GetUserName();

    /// <summary>
    /// Sets an audit-name override for unauthenticated operations (e.g. registration).
    /// Scoped per request; takes precedence over claims-based resolution.
    /// </summary>
    void SetAuditOverride(string name);
}
