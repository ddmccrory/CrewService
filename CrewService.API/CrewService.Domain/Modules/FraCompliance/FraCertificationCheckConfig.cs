using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

/// <summary>
/// Railroad-configurable per-check-type settings for FRA certification eligibility checks.
/// Railroad-level rows override parent-level defaults.
/// OperationalMonitoring and ComplianceTest have IsEnforcementLocked = true and are always enforced.
/// </summary>
public sealed class FraCertificationCheckConfig : Entity
{
    public ControlNumber ParentCtrlNbr { get; private set; }
    public ControlNumber? RailroadCtrlNbr { get; private set; }
    public string CheckType { get; private set; } = string.Empty;
    public int StalenessLimitDays { get; private set; }
    public bool IsEnforced { get; private set; }

    /// <summary>
    /// When true, IsEnforced cannot be disabled (applies to OperationalMonitoring and ComplianceTest).
    /// </summary>
    public bool IsEnforcementLocked { get; private set; }

    private FraCertificationCheckConfig()
    {
        ParentCtrlNbr = null!;
    }

    public static FraCertificationCheckConfig Create(
        ControlNumber parentCtrlNbr,
        ControlNumber? railroadCtrlNbr,
        string checkType,
        int stalenessLimitDays,
        bool isEnforced,
        bool isEnforcementLocked = false)
    {
        return new FraCertificationCheckConfig
        {
            ParentCtrlNbr = parentCtrlNbr,
            RailroadCtrlNbr = railroadCtrlNbr,
            CheckType = checkType,
            StalenessLimitDays = stalenessLimitDays,
            IsEnforced = isEnforced,
            IsEnforcementLocked = isEnforcementLocked
        };
    }

    public void Update(int stalenessLimitDays, bool isEnforced)
    {
        StalenessLimitDays = stalenessLimitDays;
        // Cannot disable enforcement for locked check types
        IsEnforced = IsEnforcementLocked ? true : isEnforced;
    }
}
