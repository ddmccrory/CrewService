using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

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
        var result = await EvaluateAsync(employeeCtrlNbr, qualificationType, uow, ct);
        await uow.CommitAsync(ct);
        return result;
    }

    public async Task<RequirementEvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationType qualificationType,
        IOrchestrationUnitOfWork uow,
        CancellationToken ct = default)
    {

        var prerequisites = await uow.QualificationRequirements
            .GetByQualificationTypeCtrlNbrAsync(qualificationType.CtrlNbr);

        if (prerequisites.Count == 0)
        {
            return new RequirementEvaluationResult(
                AllSatisfied: true,
                Results: [],
                QualificationTypeCtrlNbr: qualificationType.CtrlNbr,
                QualificationCreated: false);
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
                    PendingUntil: result.PendingUntil));

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

        // Always create or update the record.
        // AchievedAtUtc is null (Pending) until requirements are met, then set to the earned date.
        // For time-based requirements not yet met, AchievedAtUtc is the future date they will be earned.
        var qualificationCreated = false;
        var existingQualification = await uow.EmployeeQualifications
            .GetByEmployeeAndTypeAsync(employeeCtrlNbr, qualificationType.CtrlNbr);

        DateTime? achievedAtUtc = null;
        if (allSatisfied)
        {
            achievedAtUtc = DateTime.UtcNow;
        }
        else
        {
            // Use the latest known future date for time-based requirements; null for cert/manual
            var latestPending = results
                .Where(r => !r.IsSatisfied && r.PendingUntil.HasValue)
                .Select(r => r.PendingUntil!.Value)
                .Cast<DateTime?>()
                .DefaultIfEmpty(null)
                .Max();
            achievedAtUtc = latestPending; // null if no computable future date (e.g. cert not yet held)
        }

        if (existingQualification is null)
        {
            var expiresAtUtc = allSatisfied ? ComputeExpirationUtc(qualificationType, achievedAtUtc!.Value) : null;
            var createdQualification = EmployeeQualification.Create(
                employeeCtrlNbr,
                qualificationType.CtrlNbr,
                SystemActors.System,
                expiresAtUtc,
                achievedAtUtc);

            foreach (var check in results)
            {
                createdQualification.AddEvidence(
                    MapEvidenceType(check.Kind),
                    check.Description,
                    SystemActors.System,
                    check.RequirementCtrlNbr);
            }

            await uow.EmployeeQualifications.AddAsync(createdQualification, ct);
            await uow.SaveAsync(ct);
            qualificationCreated = true;
        }
        else if (existingQualification.Status == QualificationStatuses.Pending && allSatisfied)
        {
            // Requirements now satisfied — activate the existing pending record
            existingQualification.Activate(achievedAtUtc!.Value, ComputeExpirationUtc(qualificationType, achievedAtUtc.Value));
            foreach (var check in results.Where(r => r.IsSatisfied))
            {
                existingQualification.AddEvidence(
                    MapEvidenceType(check.Kind),
                    check.Description,
                    SystemActors.System,
                    check.RequirementCtrlNbr);
            }
            await uow.EmployeeQualifications.UpdateAsync(existingQualification, ct);
            await uow.SaveAsync(ct);
        }

        return new RequirementEvaluationResult(
            AllSatisfied: allSatisfied,
            Results: results,
            QualificationTypeCtrlNbr: qualificationType.CtrlNbr,
            QualificationCreated: qualificationCreated);
    }

    private static DateTime? ComputeExpirationUtc(QualificationType qualificationType, DateTime achievedAtUtc)
    {
        if (!qualificationType.ExpirationMonths.HasValue)
            return null;

        var baseExpiration = achievedAtUtc.AddMonths(qualificationType.ExpirationMonths.Value);

        if (!qualificationType.CalendarYearExpiry)
            return baseExpiration;

        return new DateTime(baseExpiration.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    }

    private static string MapEvidenceType(string requirementKind) => requirementKind switch
    {
        RequirementKinds.TimeFromEvent => EvidenceTypes.TimeThresholdMet,
        RequirementKinds.ActivityCount => EvidenceTypes.ActivityCountMet,
        RequirementKinds.TimeInRole => EvidenceTypes.TimeThresholdMet,
        RequirementKinds.QualificationHeld => EvidenceTypes.QualificationHeld,
        RequirementKinds.FraCertificationHeld => EvidenceTypes.FraCertificationHeld,
        _ => EvidenceTypes.ManualCompletion
    };
}

public sealed record RequirementEvaluationResult(
    bool AllSatisfied,
    IReadOnlyList<RequirementCheckResult> Results,
    ControlNumber QualificationTypeCtrlNbr,
    bool QualificationCreated);

public sealed record RequirementCheckResult(
    ControlNumber RequirementCtrlNbr,
    string Kind,
    bool IsSatisfied,
    string Description,
    DateTime? PendingUntil = null);
