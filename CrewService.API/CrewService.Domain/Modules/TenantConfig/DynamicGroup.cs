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
    public ControlNumber? ParentCtrlNbr { get; private set; }
    public ControlNumber? RailroadCtrlNbr { get; private set; }
    /// <summary>
    /// IANA or Windows timezone identifier for this work area (e.g. "America/Chicago" or "Central Standard Time").
    /// Only meaningful when <see cref="IsWorkArea"/> is <c>true</c>.
    /// Used to convert bulletin times between local work-area time and UTC.
    /// </summary>
    public string? TimeZoneId { get; private set; }

    /// <summary>
    /// How this railroad's "work period" is calculated for on-duty history filtering.
    /// Only meaningful on the railroad group. Defaults to <see cref="WorkPeriodMode.HalfMonth"/>
    /// to preserve legacy pay-period behavior (1st–15th and 16th–end-of-month).
    /// </summary>
    public WorkPeriodMode WorkPeriodMode { get; private set; } = WorkPeriodMode.HalfMonth;

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
        ControlNumber? parentCtrlNbr,
        ControlNumber? railroadCtrlNbr,
        string? timeZoneId,
        WorkPeriodMode? workPeriodMode)
    {
        GroupTypeCtrlNbr = groupTypeCtrlNbr;
        Name = name;
        Code = code;
        ParentGroupCtrlNbr = parentGroupCtrlNbr;
        Path = path;
        IsWorkArea = isWorkArea;
        ParentCtrlNbr = parentCtrlNbr;
        RailroadCtrlNbr = railroadCtrlNbr;
        TimeZoneId = timeZoneId;
        WorkPeriodMode = workPeriodMode ?? WorkPeriodMode.HalfMonth;
    }

    public static DynamicGroup Create(
        ControlNumber groupTypeCtrlNbr,
        string name,
        ControlNumber? parentGroupCtrlNbr,
        string? path,
        bool isWorkArea,
        string? code = null,
        ControlNumber? parentCtrlNbr = null,
        ControlNumber? railroadCtrlNbr = null,
        string? timeZoneId = null,
        WorkPeriodMode? workPeriodMode = null)
    {
        var group = new DynamicGroup(
            groupTypeCtrlNbr,
            name,
            code,
            parentGroupCtrlNbr,
            path,
            isWorkArea,
            parentCtrlNbr,
            railroadCtrlNbr,
            timeZoneId,
            workPeriodMode);
        group.Raise(new DynamicGroupCreatedDomainEvent(group));
        return group;
    }

    public void Update(string name, ControlNumber? parentGroupCtrlNbr, string? path, bool isWorkArea, string? code = null, ControlNumber? parentCtrlNbr = null, ControlNumber? railroadCtrlNbr = null, string? timeZoneId = null, WorkPeriodMode? workPeriodMode = null)
    {
        Name = name;
        Code = code;
        ParentGroupCtrlNbr = parentGroupCtrlNbr;
        Path = path;
        IsWorkArea = isWorkArea;
        ParentCtrlNbr = parentCtrlNbr;
        RailroadCtrlNbr = railroadCtrlNbr;
        TimeZoneId = timeZoneId;
        WorkPeriodMode = workPeriodMode ?? WorkPeriodMode.HalfMonth;
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
