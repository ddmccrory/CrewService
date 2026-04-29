using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

public interface IRequirementEvaluator
{
    string Kind { get; }
    Task<EvaluationResult> EvaluateAsync(ControlNumber employeeCtrlNbr, QualificationRequirement rule, CancellationToken ct = default);
}

public sealed record EvaluationResult(bool IsSatisfied, string Description, DateTime? PendingUntil = null, string? FailureKind = null, long? RelatedCertificationCtrlNbr = null, string? SatisfiedStatus = null)
{
    public static EvaluationResult Satisfied(string description, long? relatedCertificationCtrlNbr = null, string? satisfiedStatus = null) =>
        new(true, description, RelatedCertificationCtrlNbr: relatedCertificationCtrlNbr, SatisfiedStatus: satisfiedStatus);
    public static EvaluationResult NotSatisfied(string description, DateTime? pendingUntil = null, string? failureKind = null, long? relatedCertificationCtrlNbr = null) =>
        new(false, description, pendingUntil, failureKind, relatedCertificationCtrlNbr);
    public static EvaluationResult RequiresManualAction(string description) => new(false, description);
}
