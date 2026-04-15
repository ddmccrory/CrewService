using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

public sealed class PrerequisiteEvaluationService(
    IEnumerable<IPrerequisiteEvaluator> evaluators,
    IQualificationPrerequisiteRepository prerequisiteRepository,
    IEmployeeQualificationRepository employeeQualificationRepository)
{
    private readonly Dictionary<string, IPrerequisiteEvaluator> _evaluatorMap =
        evaluators.ToDictionary(e => e.Kind, StringComparer.OrdinalIgnoreCase);

    public async Task<PrerequisiteEvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationType qualificationType,
        CancellationToken ct = default)
    {
        var prerequisites = await prerequisiteRepository
            .GetByQualificationTypeCtrlNbrAsync(qualificationType.CtrlNbr);

        if (prerequisites.Count == 0)
        {
            return new PrerequisiteEvaluationResult(
                AllSatisfied: true,
                Results: [],
                QualificationTypeCtrlNbr: qualificationType.CtrlNbr,
                QualificationCreated: false);
        }

        var results = new List<PrerequisiteCheckResult>();
        var allSatisfied = true;

        foreach (var prerequisite in prerequisites)
        {
            if (_evaluatorMap.TryGetValue(prerequisite.PrerequisiteKind, out var evaluator))
            {
                var result = await evaluator.EvaluateAsync(employeeCtrlNbr, prerequisite, ct);
                results.Add(new PrerequisiteCheckResult(
                    PrerequisiteCtrlNbr: prerequisite.CtrlNbr,
                    Kind: prerequisite.PrerequisiteKind,
                    IsSatisfied: result.IsSatisfied,
                    Description: result.Description));

                if (!result.IsSatisfied)
                    allSatisfied = false;
            }
            else
            {
                results.Add(new PrerequisiteCheckResult(
                    PrerequisiteCtrlNbr: prerequisite.CtrlNbr,
                    Kind: prerequisite.PrerequisiteKind,
                    IsSatisfied: false,
                    Description: $"No evaluator registered for kind '{prerequisite.PrerequisiteKind}'"));
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
                    "SYSTEM",
                    expiresAtUtc,
                    status: "Pending");

                foreach (var check in results.Where(r => r.IsSatisfied))
                {
                    createdQualification.AddEvidence(
                        MapEvidenceType(check.Kind),
                        check.Description,
                        "SYSTEM",
                        check.PrerequisiteCtrlNbr);
                }

                await employeeQualificationRepository.AddAsync(createdQualification, ct);
                qualificationCreated = true;
            }
        }

        return new PrerequisiteEvaluationResult(
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

    private static string MapEvidenceType(string prerequisiteKind) => prerequisiteKind switch
    {
        "TimeFromEvent" => "TimeThresholdMet",
        "ActivityCount" => "ActivityCountMet",
        "TimeInRole" => "TimeThresholdMet",
        "QualificationHeld" => "QualificationHeld",
        _ => "ManualCompletion"
    };
}

public sealed record PrerequisiteEvaluationResult(
    bool AllSatisfied,
    IReadOnlyList<PrerequisiteCheckResult> Results,
    ControlNumber QualificationTypeCtrlNbr,
    bool QualificationCreated);

public sealed record PrerequisiteCheckResult(
    ControlNumber PrerequisiteCtrlNbr,
    string Kind,
    bool IsSatisfied,
    string Description);
