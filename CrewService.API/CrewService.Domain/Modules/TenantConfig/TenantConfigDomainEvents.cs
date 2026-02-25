using CrewService.Domain.DomainEvents;

namespace CrewService.Domain.Modules.TenantConfig;

public sealed record GroupTypeCreatedDomainEvent : DomainEvent
{
    public GroupTypeCreatedDomainEvent(GroupType groupType)
        : base(nameof(GroupType), groupType.CtrlNbr.Value, new { groupType.Name, groupType.IsWorkArea }) { }
}

public sealed record GroupTypeUpdatedDomainEvent : DomainEvent
{
    public GroupTypeUpdatedDomainEvent(GroupType groupType)
        : base(nameof(GroupType), groupType.CtrlNbr.Value, new { groupType.Name, groupType.IsWorkArea }) { }
}

public sealed record DynamicGroupCreatedDomainEvent : DomainEvent
{
    public DynamicGroupCreatedDomainEvent(DynamicGroup group)
        : base(nameof(DynamicGroup), group.CtrlNbr.Value, new { group.Name, group.IsWorkArea, ParentGroupCtrlNbr = group.ParentGroupCtrlNbr?.Value }) { }
}

public sealed record DynamicGroupUpdatedDomainEvent : DomainEvent
{
    public DynamicGroupUpdatedDomainEvent(DynamicGroup group)
        : base(nameof(DynamicGroup), group.CtrlNbr.Value, new { group.Name, group.IsWorkArea, ParentGroupCtrlNbr = group.ParentGroupCtrlNbr?.Value }) { }
}

public sealed record RailroadPlacedInGroupDomainEvent : DomainEvent
{
    public RailroadPlacedInGroupDomainEvent(RailroadGroupPlacement placement)
        : base(nameof(RailroadGroupPlacement), placement.CtrlNbr.Value, new { RailroadCtrlNbr = placement.RailroadCtrlNbr.Value, GroupCtrlNbr = placement.GroupCtrlNbr.Value }) { }
}

public sealed record RailroadRemovedFromGroupDomainEvent : DomainEvent
{
    public RailroadRemovedFromGroupDomainEvent(long placementCtrlNbr, long railroadCtrlNbr, long groupCtrlNbr)
        : base(nameof(RailroadGroupPlacement), placementCtrlNbr, new { RailroadCtrlNbr = railroadCtrlNbr, GroupCtrlNbr = groupCtrlNbr }) { }
}
