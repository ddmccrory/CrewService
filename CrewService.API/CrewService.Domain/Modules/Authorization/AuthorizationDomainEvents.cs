using CrewService.Domain.DomainEvents;

namespace CrewService.Domain.Modules.Authorization;

public sealed record RoleCreatedDomainEvent : DomainEvent
{
    public RoleCreatedDomainEvent(Role role)
        : base(nameof(Role), role.CtrlNbr.Value, new { role.Name, role.Level, role.IsSystem }) { }
}

public sealed record RoleUpdatedDomainEvent : DomainEvent
{
    public RoleUpdatedDomainEvent(Role role)
        : base(nameof(Role), role.CtrlNbr.Value, new { role.Name, role.Level }) { }
}

public sealed record PermissionUpdatedDomainEvent : DomainEvent
{
    public PermissionUpdatedDomainEvent(Permission permission)
        : base(nameof(Permission), permission.CtrlNbr.Value, new { RoleCtrlNbr = permission.RoleCtrlNbr.Value, FeatureCtrlNbr = permission.FeatureCtrlNbr.Value, permission.AccessLevel, permission.ParentCtrlNbr }) { }
}
