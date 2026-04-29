using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

/// <summary>
/// Pure compute engine -- evaluates requirements for a qualification type against an employee's
/// ground-truth data. Never reads or writes EmployeeQualification rows.
/// </summary>
public sealed class RequirementEvaluationService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IEnumerable<IRequirementEvaluator> evaluators)
{
    private readonly Dictionary<string, IRequirementEvaluator> _evaluatorMap =
        evaluators.ToDictionary(e => e.Kind, StringComparer.OrdinalIgnoreCase);

    public async Task<RequirementEvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationType qualificationType,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await EvaluateAsync(employeeCtrlNbr, qualificationType, uow, ct);
    }

    public async Task<RequirementEvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationType qualificationType,
        IOrchestrationUnitOfWork uow,
        CancellationToken ct = default)
    {
        // Check for an active supervisor suspension first -- short-circuits all requirement evaluation
        var suspension = await uow.QualificationSuspensions
            .GetActiveByEmployeeAndTypeAsync(employeeCtrlNbr, qualificationType.CtrlNbr, ct);

        if (suspension is not null)
        {
            var suspendedUntil = suspension.AutoReinstateAtUtc.HasValue
                ? $" until {suspension.AutoReinstateAtUtc.Value:yyyy-MM-dd}"
                : string.Empty;
            return new RequirementEvaluationResult(
                AllSatisfied: false,
                Results: [new RequirementCheckResult(
                    RequirementCtrlNbr: suspension.CtrlNbr,
                    Kind: RequirementKinds.Suspended,
                    IsSatisfied: false,
                    Description: $"Suspended by {suspension.SuspendedBy}: {suspension.Reason}{suspendedUntil}")],
                QualificationTypeCtrlNbr: qualificationType.CtrlNbr,
                IsSuspended: true,
                SuspensionCtrlNbr: suspension.CtrlNbr,
                SuspensionReason: suspension.Reason,
                AutoReinstateAtUtc: suspension.AutoReinstateAtUtc);
        }

        var prerequisites = await uow.QualificationRequirements
            .GetByQualificationTypeCtrlNbrAsync(qualificationType.CtrlNbr);

        if (prerequisites.Count == 0)
        {
            return new RequirementEvaluationResult(
                AllSatisfied: true,
                Results: [],
                QualificationTypeCtrlNbr: qualificationType.CtrlNbr);
        }

        var results = new List<RequirementCheckResult>();
        var allSatisfied = true;

        foreach (var prerequisite in prerequisites)
        {
            if (_evaluatorMap.TryGetValue(prerequisite.RequirementKind, out var evaluator))
            {
                var result = await evaluator.EvaluateAsync(employeeCtrlNbr, prerequisite, ct);
                results.Add(new RequirementCheckResult(
                    RequirementCtrlNbr: prerequisite.CtrlNbr,
                    Kind: prerequisite.RequirementKind,
                    IsSatisfied: result.IsSatisfied,
                    Description: result.Description,
                    PendingUntil: result.PendingUntil,
                    FailureKind: result.FailureKind,
                    RelatedCertificationCtrlNbr: result.RelatedCertificationCtrlNbr > 0 ? result.RelatedCertificationCtrlNbr : null,
                    SatisfiedStatus: result.SatisfiedStatus));
                if (!result.IsSatisfied)
                    allSatisfied = false;
            }
            else
            {
                results.Add(new RequirementCheckResult(
                    RequirementCtrlNbr: prerequisite.CtrlNbr,
                    Kind: prerequisite.RequirementKind,
                    IsSatisfied: false,
                    Description: $"No evaluator registered for kind '{prerequisite.RequirementKind}'"));
                allSatisfied = false;
            }
        }

        string? overallFailureKind = allSatisfied ? null
            : results.Where(r => !r.IsSatisfied && r.FailureKind is not null).Select(r => r.FailureKind).FirstOrDefault();
        string? overallSatisfiedStatus = allSatisfied
            ? results.Where(r => r.IsSatisfied && r.SatisfiedStatus is not null).Select(r => r.SatisfiedStatus).FirstOrDefault()
            : null;
        long? relatedCertificationCtrlNbr = results
            .Where(r => r.RelatedCertificationCtrlNbr.HasValue)
            .Select(r => r.RelatedCertificationCtrlNbr)
            .FirstOrDefault();
        DateTime? achievedAtUtc = allSatisfied ? DateTime.UtcNow : null;
        DateTime? expiresAtUtc = allSatisfied ? ComputeExpirationUtc(qualificationType, achievedAtUtc!.Value) : null;

        return new RequirementEvaluationResult(
            AllSatisfied: allSatisfied,
            Results: results,
            QualificationTypeCtrlNbr: qualificationType.CtrlNbr,
            AchievedAtUtc: achievedAtUtc,
            ExpiresAtUtc: expiresAtUtc,
            OverallFailureKind: overallFailureKind,
            SatisfiedStatus: overallSatisfiedStatus,
            RelatedCertificationCtrlNbr: relatedCertificationCtrlNbr);
    }

    internal static DateTime? ComputeExpirationUtc(QualificationType qualificationType, DateTime achievedAtUtc)
    {
        if (!qualificationType.ExpirationMonths.HasValue)
            return null;

        var baseExpiration = achievedAtUtc.AddMonths(qualificationType.ExpirationMonths.Value);

        return qualificationType.CalendarYearExpiry
            ? new DateTime(baseExpiration.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc)
            : baseExpiration;
    }
}

public sealed record RequirementEvaluationResult(
    bool AllSatisfied,
    IReadOnlyList<RequirementCheckResult> Results,
    ControlNumber QualificationTypeCtrlNbr,
    DateTime? AchievedAtUtc = null,
    DateTime? ExpiresAtUtc = null,
    bool IsSuspended = false,
    ControlNumber? SuspensionCtrlNbr = null,
    string? SuspensionReason = null,
    DateTime? AutoReinstateAtUtc = null,
    string? OverallFailureKind = null,
    string? SatisfiedStatus = null,
    long? RelatedCertificationCtrlNbr = null);

public sealed record RequirementCheckResult(
    ControlNumber RequirementCtrlNbr,
    string Kind,
    bool IsSatisfied,
    string Description,
    DateTime? PendingUntil = null,
    string? FailureKind = null,
    long? RelatedCertificationCtrlNbr = null,
    string? SatisfiedStatus = null);