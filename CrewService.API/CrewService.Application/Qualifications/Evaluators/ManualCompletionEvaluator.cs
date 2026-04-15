using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications.Evaluators;

public sealed class ManualCompletionEvaluator : IPrerequisiteEvaluator
{
    public string Kind => "Manual";

    public Task<EvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationPrerequisite rule,
        CancellationToken ct = default)
    {
        return Task.FromResult(
            EvaluationResult.RequiresManualAction("Requires admin to record completion manually"));
    }
}
