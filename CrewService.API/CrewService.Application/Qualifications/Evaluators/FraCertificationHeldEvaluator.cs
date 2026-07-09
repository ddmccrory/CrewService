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
            employeeCtrlNbr, rule.RequiredRegulatoryQualCtrlNbr, ct);

        if (cert is null)
            return EvaluationResult.NotSatisfied("Required FRA certification not held");

        var certCtrlNbr = cert.CtrlNbr.Value;

        if (cert.Status == CertificationStatuses.Expired)
            return EvaluationResult.NotSatisfied(
                $"FRA certification expired on {cert.ExpirationDate:yyyy-MM-dd}",
                failureKind: QualificationStatuses.Expired,
                relatedCertificationCtrlNbr: certCtrlNbr);

        if (cert.Status is CertificationStatuses.Suspended or CertificationStatuses.Revoked)
            return EvaluationResult.NotSatisfied(
                $"FRA certification is {cert.Status}",
                failureKind: cert.Status,
                relatedCertificationCtrlNbr: certCtrlNbr);

        // Translate cert status into qualification vocabulary here, at the source.
        // Renew = cert still valid but due for renewal; Active = fully current.
        // Any other future cert status defaults to Active.
        string qualStatus = cert.Status == CertificationStatuses.Renew
            ? QualificationStatuses.Renew
            : QualificationStatuses.Active;

        return EvaluationResult.Satisfied(
            $"Holds FRA certification ({cert.Status}, certified {cert.CertificationDate:yyyy-MM-dd})",
            relatedCertificationCtrlNbr: certCtrlNbr,
            satisfiedStatus: qualStatus);
    }
}