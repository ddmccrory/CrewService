using CrewService.Domain.Primitives;

namespace CrewService.Domain.Modules.Authorization;

public sealed class Feature : Entity
{
    public string Key { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Route { get; private set; } = string.Empty;

    private Feature() { }

    private Feature(string key, string displayName, string category, string route)
    {
        Key = key;
        DisplayName = displayName;
        Category = category;
        Route = route;
    }

    public static Feature Create(string key, string displayName, string category, string route)
    {
        return new Feature(key, displayName, category, route);
    }

    public void Update(string displayName, string category, string route)
    {
        DisplayName = displayName;
        Category = category;
        Route = route;
    }
}
