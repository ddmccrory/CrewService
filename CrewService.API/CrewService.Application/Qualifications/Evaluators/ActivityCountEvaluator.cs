using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications.Evaluators;

public interface IOnDutyRecordCounter
{
    Task<int> CountCompletedAsync(ControlNumber employeeCtrlNbr, string? activityFilter = null, CancellationToken ct = default);
}

public sealed class ActivityCountEvaluator(IOnDutyRecordCounter onDutyRecordCounter) : IPrerequisiteEvaluator
{
    public string Kind => "ActivityCount";

    public async Task<EvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationPrerequisite rule,
        CancellationToken ct = default)
    {
        var count = await onDutyRecordCounter.CountCompletedAsync(employeeCtrlNbr, rule.ActivityFilter, ct);
        var met = count >= rule.Threshold;

        var description = $"{count} qualifying on-duty records";

        return met
            ? EvaluationResult.Satisfied(description)
            : EvaluationResult.NotSatisfied($"{description} — need {rule.Threshold}");
    }
}
