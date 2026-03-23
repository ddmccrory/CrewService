using CrewService.Domain.Primitives;

namespace CrewService.Domain.Modules.TenantConfig;

public sealed class GroupType : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsWorkArea { get; private set; }
    public string? FlagsJson { get; private set; }
    public long ParentCtrlNbr { get; private set; }
    public long RailroadCtrlNbr { get; private set; }
    public long ParentGroupTypeCtrlNbr { get; private set; }

    private GroupType() { }

    private GroupType(string name, string? description, bool isWorkArea, string? flagsJson, long parentCtrlNbr, long railroadCtrlNbr, long parentGroupTypeCtrlNbr)
    {
        Name = name;
        Description = description;
        IsWorkArea = isWorkArea;
        FlagsJson = flagsJson;
        ParentCtrlNbr = parentCtrlNbr;
        RailroadCtrlNbr = railroadCtrlNbr;
        ParentGroupTypeCtrlNbr = parentGroupTypeCtrlNbr;
    }

    public static GroupType Create(string name, string? description, bool isWorkArea, string? flagsJson = null, long parentCtrlNbr = 0, long railroadCtrlNbr = 0, long parentGroupTypeCtrlNbr = 0)
    {
        var groupType = new GroupType(name, description, isWorkArea, flagsJson, parentCtrlNbr, railroadCtrlNbr, parentGroupTypeCtrlNbr);
        groupType.Raise(new GroupTypeCreatedDomainEvent(groupType));
        return groupType;
    }

    public void Update(string name, string? description, bool isWorkArea, string? flagsJson, long parentCtrlNbr = 0, long railroadCtrlNbr = 0, long parentGroupTypeCtrlNbr = 0)
    {
        Name = name;
        Description = description;
        IsWorkArea = isWorkArea;
        FlagsJson = flagsJson;
        ParentCtrlNbr = parentCtrlNbr;
        RailroadCtrlNbr = railroadCtrlNbr;
        ParentGroupTypeCtrlNbr = parentGroupTypeCtrlNbr;
        Raise(new GroupTypeUpdatedDomainEvent(this));
    }
}
