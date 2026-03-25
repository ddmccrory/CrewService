using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Authorization;

public sealed class Permission : Entity
{
    public ControlNumber RoleCtrlNbr { get; private set; } = null!;
    public ControlNumber FeatureCtrlNbr { get; private set; } = null!;
    public AccessLevel AccessLevel { get; private set; }
    public long? ParentCtrlNbr { get; private set; }

    private Permission() { }

    private Permission(ControlNumber roleCtrlNbr, ControlNumber featureCtrlNbr, AccessLevel accessLevel, long? parentCtrlNbr)
    {
        RoleCtrlNbr = roleCtrlNbr;
        FeatureCtrlNbr = featureCtrlNbr;
        AccessLevel = accessLevel;
        ParentCtrlNbr = parentCtrlNbr;
    }

    public static Permission Create(ControlNumber roleCtrlNbr, ControlNumber featureCtrlNbr, AccessLevel accessLevel, long? parentCtrlNbr = null)
    {
        return new Permission(roleCtrlNbr, featureCtrlNbr, accessLevel, parentCtrlNbr);
    }

    public void UpdateAccessLevel(AccessLevel accessLevel)
    {
        AccessLevel = accessLevel;
        Raise(new PermissionUpdatedDomainEvent(this));
    }
}
