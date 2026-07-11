using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Policies;

public sealed class CraftOperationsPolicy : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }
    public int LateCallThresholdMinutes { get; private set; }
    public string RestCalculationStrategy { get; private set; } = "FRA";
    public decimal? FixedRestHours { get; private set; }
    public decimal ConsecutiveDayResetHours { get; private set; }
    public bool DeleteConflictingNextShift { get; private set; }
    public bool AutoAnnulCreatesOffDuty { get; private set; }
    public bool HangoutAutoMoveEnabled { get; private set; }
    public string HangoutAutoMoveTargetBoardType { get; private set; } = "ExtraBoard";
    public int HangoutAutoMoveDelayHours { get; private set; }

    private CraftOperationsPolicy()
    {
        CraftCtrlNbr = null!;
    }

    public static CraftOperationsPolicy Create(
        ControlNumber craftCtrlNbr,
        int lateCallThresholdMinutes = 90,
        string restCalculationStrategy = "FRA",
        decimal? fixedRestHours = null,
        decimal consecutiveDayResetHours = 24m,
        bool deleteConflictingNextShift = false,
        bool autoAnnulCreatesOffDuty = false,
        bool hangoutAutoMoveEnabled = false,
        string hangoutAutoMoveTargetBoardType = "ExtraBoard",
        int hangoutAutoMoveDelayHours = 48)
    {
        return new CraftOperationsPolicy
        {
            CraftCtrlNbr = craftCtrlNbr,
            LateCallThresholdMinutes = lateCallThresholdMinutes,
            RestCalculationStrategy = restCalculationStrategy,
            FixedRestHours = fixedRestHours,
            ConsecutiveDayResetHours = consecutiveDayResetHours,
            DeleteConflictingNextShift = deleteConflictingNextShift,
            AutoAnnulCreatesOffDuty = autoAnnulCreatesOffDuty,
            HangoutAutoMoveEnabled = hangoutAutoMoveEnabled,
            HangoutAutoMoveTargetBoardType = hangoutAutoMoveTargetBoardType,
            HangoutAutoMoveDelayHours = hangoutAutoMoveDelayHours
        };
    }

    public void Update(
        int? lateCallThresholdMinutes = null,
        string? restCalculationStrategy = null,
        decimal? fixedRestHours = null,
        decimal? consecutiveDayResetHours = null,
        bool? deleteConflictingNextShift = null,
        bool? autoAnnulCreatesOffDuty = null,
        bool? hangoutAutoMoveEnabled = null,
        string? hangoutAutoMoveTargetBoardType = null,
        int? hangoutAutoMoveDelayHours = null)
    {
        if (lateCallThresholdMinutes.HasValue) LateCallThresholdMinutes = lateCallThresholdMinutes.Value;
        if (restCalculationStrategy is not null) RestCalculationStrategy = restCalculationStrategy;
        if (fixedRestHours.HasValue) FixedRestHours = fixedRestHours;
        if (consecutiveDayResetHours.HasValue) ConsecutiveDayResetHours = consecutiveDayResetHours.Value;
        if (deleteConflictingNextShift.HasValue) DeleteConflictingNextShift = deleteConflictingNextShift.Value;
        if (autoAnnulCreatesOffDuty.HasValue) AutoAnnulCreatesOffDuty = autoAnnulCreatesOffDuty.Value;
        if (hangoutAutoMoveEnabled.HasValue) HangoutAutoMoveEnabled = hangoutAutoMoveEnabled.Value;
        if (hangoutAutoMoveTargetBoardType is not null) HangoutAutoMoveTargetBoardType = hangoutAutoMoveTargetBoardType;
        if (hangoutAutoMoveDelayHours.HasValue) HangoutAutoMoveDelayHours = hangoutAutoMoveDelayHours.Value;
    }
}
