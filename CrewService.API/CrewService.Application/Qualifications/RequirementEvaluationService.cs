using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

public sealed class RequirementEvaluationService(
    IEnumerable<IRequirementEvaluator> evaluators,
    IQualificationRequirementRepository requirementRepository,
    IEmployeeQualificationRepository employeeQualificationRepository)
{
    private readonly Dictionary<string, IRequirementEvaluator> _evaluatorMap =
        evaluators.ToDictionary(e => e.Kind, StringComparer.OrdinalIgnoreCase);

    public async Task<RequirementEvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationType qualificationType,
        CancellationToken ct = default)
    {
        var prerequisites = await requirementRepository
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
                    Description: result.Description));

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

        var qualificationCreated = false;
        if (allSatisfied)
        {
            var existingQualification = await employeeQualificationRepository
                .GetByEmployeeAndTypeAsync(employeeCtrlNbr, qualificationType.CtrlNbr);

            if (existingQualification is null)
            {
                var expiresAtUtc = ComputeExpirationUtc(qualificationType, DateTime.UtcNow);
                var createdQualification = EmployeeQualification.Create(
                    employeeCtrlNbr,
                    qualificationType.CtrlNbr,
                    SystemActors.System,
                    expiresAtUtc,
                    status: QualificationStatuses.Pending);

                foreach (var check in results.Where(r => r.IsSatisfied))
                {
                    createdQualification.AddEvidence(
                        MapEvidenceType(check.Kind),
                        check.Description,
                        SystemActors.System,
                        check.RequirementCtrlNbr);
                }

                await employeeQualificationRepository.AddAsync(createdQualification, ct);
                qualificationCreated = true;
            }
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
    string Description);
