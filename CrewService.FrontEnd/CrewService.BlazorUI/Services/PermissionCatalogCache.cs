using CrewService.BlazorUI.Clients;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Singleton (per-server) cache for the role and feature catalogs.
/// These are global reference data that are identical for every user/circuit,
/// so they only need to be fetched once from the API and refreshed periodically.
/// The first circuit to call <see cref="GetAsync"/> populates the cache;
/// subsequent circuits reuse it until the TTL expires.
/// </summary>
public sealed class PermissionCatalogCache
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile CatalogSnapshot? _snapshot;

    /// <summary>
    /// Returns the cached catalogs, populating them from the API if missing or expired.
    /// Accepts a scoped <see cref="AuthorizationClient"/> from the caller's circuit
    /// because the gRPC client requires per-circuit auth context.
    /// </summary>
    public async Task<CatalogSnapshot> GetAsync(AuthorizationClient client)
    {
        var snapshot = _snapshot;
        if (snapshot is not null && !snapshot.IsExpired)
            return snapshot;

        await _gate.WaitAsync();
        try
        {
            // Double-check after acquiring the lock
            snapshot = _snapshot;
            if (snapshot is not null && !snapshot.IsExpired)
                return snapshot;

            var rolesTask = client.GetAllRolesAsync();
            var featuresTask = client.GetAllFeaturesAsync();
            await Task.WhenAll(rolesTask, featuresTask);

            snapshot = new CatalogSnapshot(
                rolesTask.Result.Roles.ToDictionary(r => r.Name, r => r.CtrlNbr),
                featuresTask.Result.Features.ToDictionary(f => f.CtrlNbr, f => f.Key),
                DateTime.UtcNow.Add(CacheTtl));

            _snapshot = snapshot;
            return snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    public sealed record CatalogSnapshot(
        Dictionary<string, long> RoleNameToCtrlNbr,
        Dictionary<long, string> FeatureCtrlNbrToKey,
        DateTime ExpiresUtc)
    {
        public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
    }
}
