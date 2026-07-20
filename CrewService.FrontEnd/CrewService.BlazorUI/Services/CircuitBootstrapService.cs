using System.Security.Claims;
using CrewService.BlazorUI.Client;
using CrewService.BlazorUI.Clients;
using CrewService.BlazorUI.Models.Auth;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Scoped (per-circuit) service that makes a single bootstrap gRPC call at
/// circuit startup and distributes the response to all dependent services.
/// Eliminates 4-6 sequential gRPC round-trips on login/page refresh.
/// Falls back silently — if bootstrap fails, individual services initialise normally.
/// </summary>
public sealed partial class CircuitBootstrapService(
    BootstrapClient bootstrapClient,
    CurrentUserService currentUser,
    UserPermissionService permissions,
    PermissionCatalogCache catalogCache,
    ContextOptionsService contextOptions,
    ILogger<CircuitBootstrapService> logger)
{
    private Task? _task;

    /// <summary>
    /// Ensures the bootstrap call has completed. Idempotent — concurrent
    /// callers from different components share the same in-flight Task.
    /// </summary>
    public Task EnsureInitializedAsync(ClaimsPrincipal user)
        => _task ??= InitializeCoreAsync(user);

    private async Task InitializeCoreAsync(ClaimsPrincipal user)
    {
        try
        {
            var response = await bootstrapClient.GetBootstrapDataAsync();

            // ── Seed CurrentUserService ──────────────────────────────────
            GetEmployeeResponse? employee = null;
            if (response.Employee is { Found: true })
            {
                employee = new GetEmployeeResponse
                {
                    CtrlNbr = response.Employee.CtrlNbr,
                    EmployeeNumber = response.Employee.EmployeeNumber
                };
            }
            currentUser.SeedFromBootstrap(user, employee, response.UseEmployeeProfilePath);

            // ── Seed PermissionCatalogCache (singleton) ─────────────────
            catalogCache.SeedIfEmpty(
                response.Roles.ToDictionary(r => r.Name, r => r.CtrlNbr),
                response.Features.ToDictionary(f => f.CtrlNbr, f => f.Key));

            // ── Seed UserPermissionService ───────────────────────────────
            var featureCtrlNbrToKey = response.Features
                .ToDictionary(f => f.CtrlNbr, f => f.Key);

            var initialPermissions = new Dictionary<string, int>();
            foreach (var perm in response.Permissions)
            {
                if (featureCtrlNbrToKey.TryGetValue(perm.FeatureCtrlNbr, out var key))
                {
                    if (!initialPermissions.TryGetValue(key, out var existing) || perm.AccessLevel > existing)
                        initialPermissions[key] = perm.AccessLevel;
                }
            }

            permissions.SeedFromBootstrap(
                response.Roles.ToDictionary(r => r.Name, r => r.CtrlNbr),
                featureCtrlNbrToKey,
                [.. response.UserRoleCtrlNbrs],
                response.ActiveCraft is { Found: true } ? response.ActiveCraft.CtrlNbr : 0,
                response.ActiveCraft is { Found: true } ? response.ActiveCraft.Name : null,
                initialPermissions);

            // ── Seed ContextOptionsService ───────────────────────────────
            contextOptions.SeedFromBootstrap(response.Parents);

            LogBootstrapCompleted(logger, response.Parents.Count, response.Permissions.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bootstrap call failed; services will initialize individually");
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Circuit bootstrap completed: {ParentCount} parents, {PermCount} permissions")]
    private static partial void LogBootstrapCompleted(ILogger logger, int parentCount, int permCount);
}
