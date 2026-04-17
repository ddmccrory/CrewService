using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications.Evaluators;

public sealed class QualificationHeldEvaluator(IEmployeeQualificationRepository qualificationRepository) : IPrerequisiteEvaluator
{
    public string Kind => "QualificationHeld";

    public async Task<EvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationPrerequisite rule,
        CancellationToken ct = default)
    {
        if (rule.RequiredQualTypeCtrlNbr is null)
            return EvaluationResult.NotSatisfied("No required qualification type specified");

        var existing = await qualificationRepository.GetByEmployeeAndTypeAsync(
            employeeCtrlNbr, rule.RequiredQualTypeCtrlNbr);

        if (existing is null || existing.Status is not ("Active" or "ExpiringSoon"))
            return EvaluationResult.NotSatisfied("Required qualification not held or not active");

        return EvaluationResult.Satisfied($"Holds active qualification (achieved {existing.AchievedAtUtc:yyyy-MM-dd})");
    }
}
