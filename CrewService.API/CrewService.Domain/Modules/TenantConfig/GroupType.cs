using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.TenantConfig;

public sealed class GroupType : Entity
{
    /// <summary>Names reserved for baseline-seeded system types that cannot be renamed or deleted.</summary>
    public static readonly IReadOnlySet<string> SystemTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Railroad"
    };

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsWorkArea { get; private set; }
    public string? FlagsJson { get; private set; }
    public ControlNumber? ParentCtrlNbr { get; private set; }
    public ControlNumber? RailroadCtrlNbr { get; private set; }
    public ControlNumber? ParentGroupTypeCtrlNbr { get; private set; }

    /// <summary>True when this type is a system type (Railroad) that cannot be renamed or deleted.</summary>
    public bool IsSystemType => SystemTypeNames.Contains(Name);

    private GroupType() { }

    private GroupType(string name, string? description, bool isWorkArea, string? flagsJson, ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr, ControlNumber? parentGroupTypeCtrlNbr)
    {
        Name = name;
        Description = description;
        IsWorkArea = isWorkArea;
        FlagsJson = flagsJson;
        ParentCtrlNbr = parentCtrlNbr;
        RailroadCtrlNbr = railroadCtrlNbr;
        ParentGroupTypeCtrlNbr = parentGroupTypeCtrlNbr;
    }

    public static GroupType Create(string name, string? description, bool isWorkArea, string? flagsJson = null, ControlNumber? parentCtrlNbr = null, ControlNumber? railroadCtrlNbr = null, ControlNumber? parentGroupTypeCtrlNbr = null)
    {
        var groupType = new GroupType(name, description, isWorkArea, flagsJson, parentCtrlNbr, railroadCtrlNbr, parentGroupTypeCtrlNbr);
        groupType.Raise(new GroupTypeCreatedDomainEvent(groupType));
        return groupType;
    }

    public void Update(string name, string? description, bool isWorkArea, string? flagsJson, ControlNumber? parentCtrlNbr = null, ControlNumber? railroadCtrlNbr = null, ControlNumber? parentGroupTypeCtrlNbr = null)
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
