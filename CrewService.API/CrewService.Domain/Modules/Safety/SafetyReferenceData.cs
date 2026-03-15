using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Safety;

public sealed class SafetyCategory : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private SafetyCategory() { WorkAreaGroupCtrlNbr = null!; }

    public static SafetyCategory Create(ControlNumber workAreaGroupCtrlNbr, string code, string displayName)
    {
        return new SafetyCategory
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            Code = code,
            DisplayName = displayName
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

public sealed class SafetyArea : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private SafetyArea() { WorkAreaGroupCtrlNbr = null!; }

    public static SafetyArea Create(ControlNumber workAreaGroupCtrlNbr, string code, string displayName)
    {
        return new SafetyArea
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            Code = code,
            DisplayName = displayName
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

public sealed class SafetySubdivision : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private SafetySubdivision() { WorkAreaGroupCtrlNbr = null!; }

    public static SafetySubdivision Create(ControlNumber workAreaGroupCtrlNbr, string code, string displayName)
    {
        return new SafetySubdivision
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            Code = code,
            DisplayName = displayName
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
