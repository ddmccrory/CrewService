using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.TenantConfig;

/// <summary>
/// Default <see cref="IRailroadResolver"/> implementation. Loads the work-area
/// <c>DynamicGroup</c> and defers to its <see cref="Domain.Modules.TenantConfig.DynamicGroup.OwningRailroadCtrlNbr"/>
/// invariant, so railroad resolution is identical in both the "work area references a railroad"
/// and the "railroad group is the work area" (small-railroad) topologies.
/// </summary>
public sealed class RailroadResolver : IRailroadResolver
{
    public async Task<ControlNumber?> ResolveFromWorkAreaAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        CancellationToken ct = default)
    {
        var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(workAreaGroupCtrlNbr, ct);
        return workArea?.OwningRailroadCtrlNbr;
    }
}
