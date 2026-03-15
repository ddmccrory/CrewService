using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.TenantConfig;

public sealed class RailroadGroupPlacement : Entity
{
    public ControlNumber RailroadCtrlNbr { get; private set; }
    public ControlNumber GroupCtrlNbr { get; private set; }

    private RailroadGroupPlacement()
    {
        RailroadCtrlNbr = null!;
        GroupCtrlNbr = null!;
    }

    private RailroadGroupPlacement(
        ControlNumber railroadCtrlNbr,
        ControlNumber groupCtrlNbr)
    {
        RailroadCtrlNbr = railroadCtrlNbr;
        GroupCtrlNbr = groupCtrlNbr;
    }

    public static RailroadGroupPlacement Create(
        ControlNumber railroadCtrlNbr,
        ControlNumber groupCtrlNbr)
    {
        var placement = new RailroadGroupPlacement(
            railroadCtrlNbr,
            groupCtrlNbr);
        placement.Raise(new RailroadPlacedInGroupDomainEvent(placement));
        return placement;
    }

    public void Remove()
    {
        Raise(new RailroadRemovedFromGroupDomainEvent(CtrlNbr, RailroadCtrlNbr, GroupCtrlNbr));
    }
}
