namespace CrewService.Application.VacancyAssignment.Rules;

public sealed class WorkedCapRule : ISkipRule
{
    public string RuleCode => "WORKED_CAP";
    public bool ShouldSkip(SkipRuleCandidate candidate, SkipRuleSlot slot, SkipContext ctx)
        => ctx.WorkedDayCap > 0 && ctx.RecentOnDutyCount >= ctx.WorkedDayCap;
}

public sealed class AlreadyOnDutyRule : ISkipRule
{
    public string RuleCode => "ON_DUTY";
    public bool ShouldSkip(SkipRuleCandidate candidate, SkipRuleSlot slot, SkipContext ctx)
        => ctx.HasActiveOnDuty;
}

public sealed class AvailabilityRule : ISkipRule
{
    public string RuleCode => "NOT_AVAILABLE";
    public bool ShouldSkip(SkipRuleCandidate candidate, SkipRuleSlot slot, SkipContext ctx)
        => ctx.RestedAtUtc.HasValue && ctx.RestedAtUtc.Value > ctx.NowUtc;
}

public sealed class RestRule : ISkipRule
{
    public string RuleCode => "NOT_RESTED";
    public bool ShouldSkip(SkipRuleCandidate candidate, SkipRuleSlot slot, SkipContext ctx)
        => !ctx.IsRested;
}

public sealed class MarkOffRule : ISkipRule
{
    public string RuleCode => "MARKED_OFF";
    public bool ShouldSkip(SkipRuleCandidate candidate, SkipRuleSlot slot, SkipContext ctx)
        => ctx.IsMarkedOff;
}

public sealed class QualificationRule : ISkipRule
{
    public string RuleCode => "NOT_QUALIFIED";
    public bool ShouldSkip(SkipRuleCandidate candidate, SkipRuleSlot slot, SkipContext ctx)
        => !ctx.IsQualified;
}

public sealed class WeeklyHoursCapRule : ISkipRule
{
    public string RuleCode => "WEEKLY_CAP";
    public bool ShouldSkip(SkipRuleCandidate candidate, SkipRuleSlot slot, SkipContext ctx)
        => ctx.WeeklyHoursCap > 0 && ctx.WeeklyHoursWorked >= ctx.WeeklyHoursCap;
}
