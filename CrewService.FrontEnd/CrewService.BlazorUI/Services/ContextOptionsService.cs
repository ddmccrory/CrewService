using CrewService.BlazorUI.Clients;
using CrewService.Presentation;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Scoped (per-circuit) service that fetches and caches the parent/railroad
/// options available to the current user.  Uses the server-side
/// <c>GetContextOptions</c> endpoint which returns only the parents and
/// railroads the authenticated user is authorised to see.
/// </summary>
public sealed class ContextOptionsService(BootstrapClient bootstrapClient, ILogger<ContextOptionsService> logger)
{
    private Task<List<ParentOption>>? _loadTask;

    /// <summary>
    /// Returns the filtered parent/railroad options for the current user.
    /// Idempotent — subsequent calls within the same circuit return the cached result.
    /// </summary>
    public Task<List<ParentOption>> GetOptionsAsync()
        => _loadTask ??= LoadOptionsAsync();

    /// <summary>
    /// Seeds the context options from bootstrap data, eliminating a separate
    /// <c>GetContextOptions</c> round-trip.
    /// </summary>
    public void SeedFromBootstrap(IEnumerable<ContextParent> parents)
    {
        if (_loadTask is not null) return;
        _loadTask = Task.FromResult(MapFromProto(parents));
    }

    private async Task<List<ParentOption>> LoadOptionsAsync()
    {
        try
        {
            var response = await bootstrapClient.GetContextOptionsAsync();
            return MapFromProto(response.Parents);
        }
        catch (Exception ex)
        {
            _loadTask = null; // allow retry on next call
            logger.LogError(ex, "Failed to load context options");
            throw;
        }
    }

    private static List<ParentOption> MapFromProto(IEnumerable<ContextParent> parents) =>
        parents.Select(p => new ParentOption(
            p.CtrlNbr,
            p.Name,
            [.. p.Railroads
                .Select(r => new RailroadOption(r.CtrlNbr, r.Name,
                    string.IsNullOrWhiteSpace(r.RrMark) ? r.Name : $"{r.RrMark} — {r.Name}"))
                .OrderBy(r => r.DisplayName)]))
            .OrderBy(p => p.Name)
            .ToList();

    /// <summary>A parent organization available to the current user.</summary>
    public sealed record ParentOption(long CtrlNbr, string Name, List<RailroadOption> Railroads);

    /// <summary>A railroad within a parent organization.</summary>
    public sealed record RailroadOption(long CtrlNbr, string Name, string DisplayName);
}
