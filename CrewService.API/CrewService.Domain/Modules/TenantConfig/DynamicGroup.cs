using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.TenantConfig;

public sealed class DynamicGroup : Entity
{
    public ControlNumber GroupTypeCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ControlNumber? ParentGroupCtrlNbr { get; private set; }
    public string? Path { get; private set; }
    public bool IsWorkArea { get; private set; }

    private DynamicGroup()
    {
        GroupTypeCtrlNbr = null!;
    }

    private DynamicGroup(
        ControlNumber groupTypeCtrlNbr,
        string name,
        ControlNumber? parentGroupCtrlNbr,
        string? path,
        bool isWorkArea)
    {
        GroupTypeCtrlNbr = groupTypeCtrlNbr;
        Name = name;
        ParentGroupCtrlNbr = parentGroupCtrlNbr;
        Path = path;
        IsWorkArea = isWorkArea;
    }

    public static DynamicGroup Create(
        ControlNumber groupTypeCtrlNbr,
        string name,
        ControlNumber? parentGroupCtrlNbr,
        string? path,
        bool isWorkArea)
    {
        var group = new DynamicGroup(
            groupTypeCtrlNbr,
            name,
            parentGroupCtrlNbr,
            path,
            isWorkArea);
        group.Raise(new DynamicGroupCreatedDomainEvent(group));
        return group;
    }

    public void Update(string name, ControlNumber? parentGroupCtrlNbr, string? path, bool isWorkArea)
    {
        Name = name;
        ParentGroupCtrlNbr = parentGroupCtrlNbr;
        Path = path;
        IsWorkArea = isWorkArea;
        Raise(new DynamicGroupUpdatedDomainEvent(this));
    }
}
