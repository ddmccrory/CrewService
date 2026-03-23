using CrewService.Domain.DomainEvents;
using CrewService.Domain.ValueObjects;

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
