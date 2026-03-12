using CrewService.Domain.ValueObjects;

namespace CrewService.Application.VacancyAssignment;

public interface IAssignmentStrategy
{
    string StrategyCode { get; }
    AssignmentResult TryAssign(SkipRuleCandidate candidate, SkipRuleSlot slot, AssignmentContext ctx);
}

public sealed record AssignmentResult(
    bool Success,
    ControlNumber? AssignedEmployeeCtrlNbr,
    string? FailureReason = null);

public sealed class AssignmentContext
{
    public DateTime NowUtc { get; init; } = DateTime.UtcNow;
    public bool HelperSearchEnabled { get; init; }
}

public sealed class StandardAssignmentStrategy : IAssignmentStrategy
{
    public string StrategyCode => "STANDARD";

    public AssignmentResult TryAssign(SkipRuleCandidate candidate, SkipRuleSlot slot, AssignmentContext ctx)
    {
        return new AssignmentResult(true, candidate.EmployeeCtrlNbr);
    }
}

public sealed class ForemanHelperStrategy : IAssignmentStrategy
{
    public string StrategyCode => "FOREMAN_HELPER";

    public AssignmentResult TryAssign(SkipRuleCandidate candidate, SkipRuleSlot slot, AssignmentContext ctx)
    {
        if (!ctx.HelperSearchEnabled)
            return new AssignmentResult(false, null, "Helper search not enabled for this craft");

        return new AssignmentResult(true, candidate.EmployeeCtrlNbr);
    }
}
