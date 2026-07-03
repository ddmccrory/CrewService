using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.TenantConfig;

/// <summary>
/// Resolves the owning railroad for a work-area <c>DynamicGroup</c>, centralizing the rule that
/// a work area either references its railroad via <c>RailroadCtrlNbr</c> or — when the railroad
/// group is itself the work area (common on smaller railroads, e.g. PTRA) — IS the railroad.
/// <para>
/// This replaces the <c>RailroadCtrlNbr ?? CtrlNbr</c> fallback that was previously duplicated
/// across seniority, bulletin, and notification services. Both topologies map to the domain
/// invariant <see cref="Domain.Modules.TenantConfig.DynamicGroup.OwningRailroadCtrlNbr"/>.
/// </para>
/// </summary>
public interface IRailroadResolver
{
    /// <summary>
    /// Resolves the owning railroad for the given work-area group using an existing unit of work.
    /// Returns <c>null</c> when the group cannot be found, so callers can preserve their
    /// "unresolvable railroad → skip" behavior.
    /// </summary>
    Task<ControlNumber?> ResolveFromWorkAreaAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        CancellationToken ct = default);
}
