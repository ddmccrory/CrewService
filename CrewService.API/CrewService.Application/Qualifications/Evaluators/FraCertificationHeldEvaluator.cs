using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications.Evaluators;

public sealed class FraCertificationHeldEvaluator(IEmployeeCertificationRepository certificationRepository) : IRequirementEvaluator
{
    public string Kind => RequirementKinds.FraCertificationHeld;

    public async Task<EvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationRequirement rule,
        CancellationToken ct = default)
    {
        if (rule.RequiredRegulatoryQualCtrlNbr is null)
            return EvaluationResult.NotSatisfied("No required regulatory qualification specified");

        var cert = await certificationRepository.GetByEmployeeAndRegulatoryQualAsync(
            employeeCtrlNbr, rule.RequiredRegulatoryQualCtrlNbr);

        if (cert is null || cert.Status != CertificationStatuses.Active)
            return EvaluationResult.NotSatisfied("Required FRA certification not held or not active");

        return EvaluationResult.Satisfied($"Holds active FRA certification (certified {cert.CertificationDate:yyyy-MM-dd})");
    }
}
