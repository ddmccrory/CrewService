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
        bool isWorkArea)
    {
        GroupTypeCtrlNbr = groupTypeCtrlNbr;
        Name = name;
        Code = code;
        ParentGroupCtrlNbr = parentGroupCtrlNbr;
        Path = path;
        IsWorkArea = isWorkArea;
    }

    public static DynamicGroup Create(
        ControlNumber groupTypeCtrlNbr,
        string name,
        ControlNumber? parentGroupCtrlNbr,
        string? path,
        bool isWorkArea,
        string? code = null)
    {
        var group = new DynamicGroup(
            groupTypeCtrlNbr,
            name,
            code,
            parentGroupCtrlNbr,
            path,
            isWorkArea);
        group.Raise(new DynamicGroupCreatedDomainEvent(group));
        return group;
    }

    public void Update(string name, ControlNumber? parentGroupCtrlNbr, string? path, bool isWorkArea, string? code = null)
    {
        Name = name;
        Code = code;
        ParentGroupCtrlNbr = parentGroupCtrlNbr;
        Path = path;
        IsWorkArea = isWorkArea;
        Raise(new DynamicGroupUpdatedDomainEvent(this));
    }

    /// <summary>
    /// Computes and sets the materialized <see cref="Path"/> based on the parent group's path.
    /// For root groups (no parent), the path is <c>"/{CtrlNbr}"</c>.
    /// For child groups, the path is <c>"{parentPath}/{CtrlNbr}"</c>.
    /// </summary>
    public void BuildPath(string? parentPath)
    {
        Path = parentPath is not null
            ? $"{parentPath}/{CtrlNbr.Value}"
            : $"/{CtrlNbr.Value}";
    }
}