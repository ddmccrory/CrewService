using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications.Evaluators;

public interface ICraftMembershipDateProvider
{
    Task<DateTime?> GetEarliestActiveMembershipDateAsync(ControlNumber employeeCtrlNbr, ControlNumber? craftCtrlNbr = null, CancellationToken ct = default);
}

public sealed class TimeInRoleEvaluator(ICraftMembershipDateProvider membershipDateProvider) : IPrerequisiteEvaluator
{
    public string Kind => "TimeInRole";

    public async Task<EvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationPrerequisite rule,
        CancellationToken ct = default)
    {
        var membershipDate = await membershipDateProvider.GetEarliestActiveMembershipDateAsync(employeeCtrlNbr, ct: ct);
        if (membershipDate is null)
            return EvaluationResult.NotSatisfied("No active craft/position membership found");

        var elapsed = rule.ThresholdUnit switch
        {
            "Days" => (DateTime.UtcNow - membershipDate.Value).TotalDays,
            "Months" => (DateTime.UtcNow - membershipDate.Value).TotalDays / 30.44,
            _ => 0
        };

        var met = elapsed >= rule.Threshold;
        var description = $"{(int)elapsed} {rule.ThresholdUnit.ToLowerInvariant()} in role (since {membershipDate.Value:yyyy-MM-dd})";

        return met
            ? EvaluationResult.Satisfied(description)
            : EvaluationResult.NotSatisfied($"{description} — need {rule.Threshold}");
    }
}
