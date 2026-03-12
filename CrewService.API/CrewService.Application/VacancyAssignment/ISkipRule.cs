using CrewService.Domain.ValueObjects;

namespace CrewService.Application.VacancyAssignment;

public interface ISkipRule
{
    string RuleCode { get; }
    bool ShouldSkip(SkipRuleCandidate candidate, SkipRuleSlot slot, SkipContext ctx);
}

public sealed record SkipRuleCandidate(
    ControlNumber EmployeeCtrlNbr,
    ControlNumber BoardMemberCtrlNbr,
    int OrderIndex);

public sealed record SkipRuleSlot(
    ControlNumber PositionSlotCtrlNbr,
    ControlNumber CrewPositionCtrlNbr,
    DateTime ShiftStartUtc);

public sealed class SkipContext
{
    public DateTime NowUtc { get; init; } = DateTime.UtcNow;
    public bool HasActiveOnDuty { get; init; }
    public bool IsMarkedOff { get; init; }
    public bool IsRested { get; init; }
    public bool IsQualified { get; init; }
    public int RecentOnDutyCount { get; init; }
    public decimal WeeklyHoursWorked { get; init; }
    public decimal WeeklyHoursCap { get; init; }
    public int WorkedDayCap { get; init; }
    public DateTime? RestedAtUtc { get; init; }
}
