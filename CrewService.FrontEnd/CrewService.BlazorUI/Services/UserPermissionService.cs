using System.Security.Claims;
using CrewService.BlazorUI.Clients;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Scoped (per-circuit) service that loads and caches the current user's
/// effective permissions from the authorization API. Exposes simple boolean
/// checks that the NavMenu, AppComponentBase, and individual pages use to
/// replace hardcoded <c>AuthorizeView Roles</c> checks.
/// </summary>
public sealed class UserPermissionService(
    AuthorizationClient authClient,
    SeniorityClient seniorityClient,
    CurrentUserService currentUser,
    ILogger<UserPermissionService> logger)
{
    private Dictionary<string, long> _roleNameToCtrlNbr = [];
    private Dictionary<long, string> _featureCtrlNbrToKey = [];
    private readonly Dictionary<string, int> _permissions = []; // featureKey → accessLevel
    private List<long> _userRoleCtrlNbrs = [];
    private Task? _initTask;
    private Task _loadTask = Task.CompletedTask;
    private bool _loadedOnce;
    private long? _loadedParentCtrlNbr;

    /// <summary>Raised after permissions finish loading so the NavMenu can re-render.</summary>
    public event Action? OnPermissionsLoaded;

    /// <summary>The active craft CtrlNbr resolved from the employee's last active roster, or 0 if none.</summary>
    public long ActiveCraftCtrlNbr { get; private set; }

    /// <summary>The display name of the active craft, or <c>null</c> if none.</summary>
    public string? ActiveCraftName { get; private set; }

    /// <summary><c>true</c> once roles, features, and at least one permission set have been loaded.</summary>
    public bool IsLoaded { get; private set; }

    // ── Initialization ──────────────────────────────────────────────────

    /// <summary>
    /// Loads the role and feature catalogs from the API, resolves the user's
    /// active craft, and determines which roles the current user holds.
    /// Idempotent within a circuit — concurrent callers share the same in-flight task.
    /// </summary>
    public Task InitializeAsync(ClaimsPrincipal user)
        => _initTask ??= InitializeCoreAsync(user);

    private async Task InitializeCoreAsync(ClaimsPrincipal user)
    {
        try
        {
            var rolesTask = authClient.GetAllRolesAsync();
            var featuresTask = authClient.GetAllFeaturesAsync();
            await Task.WhenAll(rolesTask, featuresTask);

            _roleNameToCtrlNbr = rolesTask.Result.Roles
                .ToDictionary(r => r.Name, r => r.CtrlNbr);

            _featureCtrlNbrToKey = featuresTask.Result.Features
                .ToDictionary(f => f.CtrlNbr, f => f.Key);

            _userRoleCtrlNbrs = [.. _roleNameToCtrlNbr
                .Where(kvp => user.IsInRole(kvp.Key))
                .Select(kvp => kvp.Value)];

            // Resolve the employee's active craft from their last active roster
            await ResolveActiveCraftAsync();
        }
        catch (Exception ex)
        {
            _initTask = null; // allow retry on next call
            logger.LogError(ex, "Failed to load role/feature catalogs");
        }
    }

    /// <summary>
    /// Loads (or reloads) the effective permissions for the current user's roles
    /// within the given parent and active craft context.
    /// Awaits initialization first so that role/feature catalogs are ready.
    /// Concurrent callers for the same parent share the same in-flight task.
    /// </summary>
    public Task LoadPermissionsAsync(long? parentCtrlNbr)
    {
        if (_loadedOnce && parentCtrlNbr == _loadedParentCtrlNbr) return _loadTask;
        _loadedOnce = true;
        _loadedParentCtrlNbr = parentCtrlNbr;
        return _loadTask = LoadPermissionsCoreAsync(parentCtrlNbr);
    }

    /// <summary>
    /// Ensures the role/feature catalogs have been loaded before the
    /// permission load proceeds.  Without this, a context-changed event
    /// that fires while <see cref="InitializeCoreAsync"/> is still in-flight
    /// would see an empty <c>_userRoleCtrlNbrs</c> list and take the
    /// early-return path, caching a zero-permission result.
    /// </summary>
    private async Task AwaitInitializationAsync()
    {
        if (_initTask is { } t) await t;
    }

    private async Task LoadPermissionsCoreAsync(long? parentCtrlNbr)
    {
        // Wait for role/feature catalogs so _userRoleCtrlNbrs is populated.
        // Without this, a context-changed event that fires while InitializeCoreAsync
        // is still in-flight would see an empty role list and cache a zero-permission result.
        await AwaitInitializationAsync();

        if (_userRoleCtrlNbrs.Count == 0)
        {
            _permissions.Clear();
            IsLoaded = true;
            OnPermissionsLoaded?.Invoke();
            return;
        }

        try
        {
            // Build in a local dictionary so _permissions stays valid while gRPC is in-flight.
            var newPermissions = new Dictionary<string, int>();

            var tasks = _userRoleCtrlNbrs.Select(roleCtrlNbr =>
                authClient.GetEffectivePermissionsAsync(
                    roleCtrlNbr, parentCtrlNbr ?? 0, ActiveCraftCtrlNbr));

            var responses = await Task.WhenAll(tasks);

            foreach (var response in responses)
            {
                foreach (var perm in response.Permissions)
                {
                    if (_featureCtrlNbrToKey.TryGetValue(perm.FeatureCtrlNbr, out var featureKey))
                    {
                        // When a user holds multiple roles, take the highest access level
                        if (!newPermissions.TryGetValue(featureKey, out var existing) || perm.AccessLevel > existing)
                        {
                            newPermissions[featureKey] = perm.AccessLevel;
                        }
                    }
                }
            }

            // Atomic swap — only replace once the full result set is ready
            _permissions.Clear();
            foreach (var kvp in newPermissions)
                _permissions[kvp.Key] = kvp.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load effective permissions for parent {ParentCtrlNbr}", parentCtrlNbr);
        }
        finally
        {
            // Always notify — even on failure — so the NavMenu re-renders
            // instead of staying stuck with a stale empty state.
            IsLoaded = true;
            OnPermissionsLoaded?.Invoke();
        }
    }

    // ── Internal helpers ────────────────────────────────────────────────

    private async Task ResolveActiveCraftAsync()
    {
        if (!currentUser.IsEmployee || currentUser.Employee is null) return;

        try
        {
            var response = await seniorityClient.GetActiveCraftAsync(currentUser.Employee.CtrlNbr);
            if (response.Found)
            {
                ActiveCraftCtrlNbr = response.CraftCtrlNbr;
                ActiveCraftName = response.CraftName;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not resolve active craft for employee {CtrlNbr}", currentUser.Employee.CtrlNbr);
        }
    }

    // ── Access checks ───────────────────────────────────────────────────

    /// <summary>Returns <c>true</c> if the user has at least ReadOnly access to the feature.</summary>
    public bool HasAccess(string featureKey) =>
        _permissions.TryGetValue(featureKey, out var level) && level > 0;

    /// <summary>Returns <c>true</c> if the user's access level is exactly ReadOnly (no create/edit/delete).</summary>
    public bool IsReadOnly(string featureKey) =>
        _permissions.TryGetValue(featureKey, out var level) && level == 1;

    /// <summary>Returns <c>true</c> if the user has FullAccess to the feature.</summary>
    public bool HasFullAccess(string featureKey) =>
        _permissions.TryGetValue(featureKey, out var level) && level == 2;

    /// <summary>Returns <c>true</c> if the user has access to at least one of the given features.</summary>
    public bool HasAccessToAny(params string[] featureKeys) =>
        featureKeys.Any(HasAccess);
}
