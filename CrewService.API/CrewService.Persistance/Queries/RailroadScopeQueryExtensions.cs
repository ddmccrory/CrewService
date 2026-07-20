using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;

namespace CrewService.Persistance.Queries;

public static class RailroadScopeQueryExtensions
{
    public static IQueryable<DynamicGroup> WhereOwnedByRailroad(
        this IQueryable<DynamicGroup> query,
        ControlNumber railroadCtrlNbr)
        => query.Where(g => g.RailroadCtrlNbr == railroadCtrlNbr
                            || (g.RailroadCtrlNbr == null && g.CtrlNbr == railroadCtrlNbr));
}
