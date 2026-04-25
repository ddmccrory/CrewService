using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications.Evaluators;

public sealed class QualificationHeldEvaluator(IEmployeeQualificationRepository qualificationRepository) : IRequirementEvaluator
{
    public string Kind => RequirementKinds.QualificationHeld;

    public async Task<EvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationRequirement rule,
        CancellationToken ct = default)
    {
        if (rule.RequiredQualTypeCtrlNbr is null)
            return EvaluationResult.NotSatisfied("No required qualification type specified");

        var existing = await qualificationRepository.GetByEmployeeAndTypeAsync(
            employeeCtrlNbr, rule.RequiredQualTypeCtrlNbr);

        if (existing is null || existing.Status is not ("Active" or "ExpiringSoon"))
            return EvaluationResult.NotSatisfied("Required qualification not held or not active");

        return EvaluationResult.Satisfied($"Holds active qualification (achieved {existing.AchievedAtUtc?.ToString("yyyy-MM-dd") ?? "pending"})");
    }
}
