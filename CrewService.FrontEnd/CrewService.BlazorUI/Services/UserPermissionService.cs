using System.Security.Claims;
using CrewService.BlazorUI.Clients;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Scoped (per-circuit) service that loads and caches the current user's
/// effective permissions from the authorization API. Exposes simple boolean
/// checks that the NavMenu, AppComponentBase, and individual pages use to
/// replace hardcoded <c>AuthorizeView Roles</c> checks.
/// </summary>
public sealed class UserPermissionService
{
    private readonly AuthorizationClient _authClient;
    private readonly SeniorityClient _seniorityClient;
    private readonly CurrentUserService _currentUser;
    private readonly ILogger<UserPermissionService> _logger;

    private Dictionary<string, long> _roleNameToCtrlNbr = new();
    private Dictionary<long, string> _featureCtrlNbrToKey = new();
    private Dictionary<string, int> _permissions = new(); // featureKey → accessLevel
    private List<long> _userRoleCtrlNbrs = [];
    private bool _initialized;
    private long? _loadedParentCtrlNbr = long.MinValue; // sentinel so first load always runs

    /// <summary>The active craft CtrlNbr resolved from the employee's last active roster, or 0 if none.</summary>
    public long ActiveCraftCtrlNbr { get; private set; }

    /// <summary>The display name of the active craft, or <c>null</c> if none.</summary>
    public string? ActiveCraftName { get; private set; }

    public UserPermissionService(AuthorizationClient authClient, SeniorityClient seniorityClient, CurrentUserService currentUser, ILogger<UserPermissionService> logger)
    {
        _authClient = authClient;
        _seniorityClient = seniorityClient;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary><c>true</c> once roles, features, and at least one permission set have been loaded.</summary>
    public bool IsLoaded { get; private set; }

    // ── Initialization ──────────────────────────────────────────────────

    /// <summary>
    /// Loads the role and feature catalogs from the API, resolves the user's
    /// active craft, and determines which roles the current user holds.
    /// Idempotent within a circuit.
    /// </summary>
    public async Task InitializeAsync(ClaimsPrincipal user)
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            var rolesTask = _authClient.GetAllRolesAsync();
            var featuresTask = _authClient.GetAllFeaturesAsync();
            await Task.WhenAll(rolesTask, featuresTask);

            _roleNameToCtrlNbr = rolesTask.Result.Roles
                .ToDictionary(r => r.Name, r => r.CtrlNbr);

            _featureCtrlNbrToKey = featuresTask.Result.Features
                .ToDictionary(f => f.CtrlNbr, f => f.Key);

            _userRoleCtrlNbrs = _roleNameToCtrlNbr
                .Where(kvp => user.IsInRole(kvp.Key))
                .Select(kvp => kvp.Value)
                .ToList();

            // Resolve the employee's active craft from their last active roster
            await ResolveActiveCraftAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load role/feature catalogs");
        }
    }

    /// <summary>
    /// Loads (or reloads) the effective permissions for the current user's roles
    /// within the given parent and active craft context.
    /// Skips redundant calls for the same parent.
    /// </summary>
    public async Task LoadPermissionsAsync(long? parentCtrlNbr)
    {
        if (parentCtrlNbr == _loadedParentCtrlNbr) return;
        _loadedParentCtrlNbr = parentCtrlNbr;
        _permissions.Clear();

        if (_userRoleCtrlNbrs.Count == 0) return;

        try
        {
            foreach (var roleCtrlNbr in _userRoleCtrlNbrs)
            {
                var response = await _authClient.GetEffectivePermissionsAsync(
                    roleCtrlNbr, parentCtrlNbr ?? 0, ActiveCraftCtrlNbr);

                foreach (var perm in response.Permissions)
                {
                    if (_featureCtrlNbrToKey.TryGetValue(perm.FeatureCtrlNbr, out var featureKey))
                    {
                        // When a user holds multiple roles, take the highest access level
                        if (!_permissions.TryGetValue(featureKey, out var existing) || perm.AccessLevel > existing)
                        {
                            _permissions[featureKey] = perm.AccessLevel;
                        }
                    }
                }
            }

            IsLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load effective permissions for parent {ParentCtrlNbr}", parentCtrlNbr);
        }
    }

    // ── Internal helpers ────────────────────────────────────────────────

    private async Task ResolveActiveCraftAsync()
    {
        if (!_currentUser.IsEmployee || _currentUser.Employee is null) return;

        try
        {
            var response = await _seniorityClient.GetActiveCraftAsync(_currentUser.Employee.CtrlNbr);
            if (response.Found)
            {
                ActiveCraftCtrlNbr = response.CraftCtrlNbr;
                ActiveCraftName = response.CraftName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not resolve active craft for employee {CtrlNbr}", _currentUser.Employee.CtrlNbr);
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
