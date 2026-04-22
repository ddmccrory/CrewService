using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications.Evaluators;

public sealed class ManualCompletionEvaluator : IRequirementEvaluator
{
    public string Kind => RequirementKinds.Manual;

    public Task<EvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationRequirement rule,
        CancellationToken ct = default)
    {
        return Task.FromResult(
            EvaluationResult.RequiresManualAction("Requires admin to record completion manually"));
    }
}
