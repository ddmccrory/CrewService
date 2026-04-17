using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications.Evaluators;

public sealed class TimeFromEventEvaluator(IEmployeeRepository employeeRepository) : IPrerequisiteEvaluator
{
    public string Kind => "TimeFromEvent";

    public async Task<EvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationPrerequisite rule,
        CancellationToken ct = default)
    {
        var employee = await employeeRepository.GetByCtrlNbrAsync(employeeCtrlNbr, ct);
        if (employee is null)
            return EvaluationResult.NotSatisfied("Employee not found");

        var eventDate = rule.EventSource switch
        {
            "EmploymentDate" => employee.EmploymentDate,
            _ => (DateTime?)null
        };

        if (eventDate is null)
            return EvaluationResult.NotSatisfied($"Unknown event source: {rule.EventSource}");

        var elapsed = rule.ThresholdUnit switch
        {
            "Days" => (DateTime.UtcNow - eventDate.Value).TotalDays,
            "Months" => (DateTime.UtcNow - eventDate.Value).TotalDays / 30.44,
            _ => 0
        };

        var met = elapsed >= rule.Threshold;
        var description = $"{(int)elapsed} {rule.ThresholdUnit.ToLowerInvariant()} since {rule.EventSource} ({eventDate.Value:yyyy-MM-dd})";

        return met
            ? EvaluationResult.Satisfied(description)
            : EvaluationResult.NotSatisfied($"{description} — need {rule.Threshold}");
    }
}
