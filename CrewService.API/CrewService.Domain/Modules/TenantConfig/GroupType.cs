using CrewService.Domain.Primitives;

namespace CrewService.Domain.Modules.TenantConfig;

public sealed class GroupType : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsWorkArea { get; private set; }
    public string? FlagsJson { get; private set; }

    private GroupType() { }

    private GroupType(string name, string? description, bool isWorkArea, string? flagsJson)
    {
        Name = name;
        Description = description;
        IsWorkArea = isWorkArea;
        FlagsJson = flagsJson;
    }

    public static GroupType Create(string name, string? description, bool isWorkArea, string? flagsJson = null)
    {
        var groupType = new GroupType(name, description, isWorkArea, flagsJson);
        groupType.Raise(new GroupTypeCreatedDomainEvent(groupType));
        return groupType;
    }

    public void Update(string name, string? description, bool isWorkArea, string? flagsJson)
    {
        Name = name;
        Description = description;
        IsWorkArea = isWorkArea;
        FlagsJson = flagsJson;
        Raise(new GroupTypeUpdatedDomainEvent(this));
    }
}
