using CrewService.Domain.Primitives;

namespace CrewService.Domain.Modules.Authorization;

public sealed class Role : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public int Level { get; private set; }

    private Role() { }

    private Role(string name, string? description, bool isSystem, int level)
    {
        Name = name;
        Description = description;
        IsSystem = isSystem;
        Level = level;
    }

    public static Role Create(string name, string? description, bool isSystem, int level)
    {
        var role = new Role(name, description, isSystem, level);
        role.Raise(new RoleCreatedDomainEvent(role));
        return role;
    }

    public void Update(string name, string? description, int level)
    {
        Name = name;
        Description = description;
        Level = level;
        Raise(new RoleUpdatedDomainEvent(this));
    }
}
