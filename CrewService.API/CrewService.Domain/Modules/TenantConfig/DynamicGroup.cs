using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.TenantConfig;

public sealed class DynamicGroup : Entity
{
    public ControlNumber GroupTypeCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public ControlNumber? ParentGroupCtrlNbr { get; private set; }
    public string? Path { get; private set; }
    public bool IsWorkArea { get; private set; }
    public long ParentCtrlNbr { get; private set; }

    private DynamicGroup()
    {
        GroupTypeCtrlNbr = null!;
    }

    private DynamicGroup(
        ControlNumber groupTypeCtrlNbr,
        string name,
        string? code,
        ControlNumber? parentGroupCtrlNbr,
        string? path,
        bool isWorkArea,
        long parentCtrlNbr)
    {
        GroupTypeCtrlNbr = groupTypeCtrlNbr;
        Name = name;
        Code = code;
        ParentGroupCtrlNbr = parentGroupCtrlNbr;
        Path = path;
        IsWorkArea = isWorkArea;
        ParentCtrlNbr = parentCtrlNbr;
    }

    public static DynamicGroup Create(
        ControlNumber groupTypeCtrlNbr,
        string name,
        ControlNumber? parentGroupCtrlNbr,
        string? path,
        bool isWorkArea,
        string? code = null,
        long parentCtrlNbr = 0)
    {
        var group = new DynamicGroup(
            groupTypeCtrlNbr,
            name,
            code,
            parentGroupCtrlNbr,
            path,
            isWorkArea,
            parentCtrlNbr);
        group.Raise(new DynamicGroupCreatedDomainEvent(group));
        return group;
    }

    public void Update(string name, ControlNumber? parentGroupCtrlNbr, string? path, bool isWorkArea, string? code = null, long parentCtrlNbr = 0)
    {
        Name = name;
        Code = code;
        ParentGroupCtrlNbr = parentGroupCtrlNbr;
        Path = path;
        IsWorkArea = isWorkArea;
        ParentCtrlNbr = parentCtrlNbr;
        Raise(new DynamicGroupUpdatedDomainEvent(this));
    }
}
