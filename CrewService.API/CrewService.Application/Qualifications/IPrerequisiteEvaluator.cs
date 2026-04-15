using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

public interface IPrerequisiteEvaluator
{
    string Kind { get; }
    Task<EvaluationResult> EvaluateAsync(ControlNumber employeeCtrlNbr, QualificationPrerequisite rule, CancellationToken ct = default);
}

public sealed record EvaluationResult(bool IsSatisfied, string Description)
{
    public static EvaluationResult Satisfied(string description) => new(true, description);
    public static EvaluationResult NotSatisfied(string description) => new(false, description);
    public static EvaluationResult RequiresManualAction(string description) => new(false, description);
}
