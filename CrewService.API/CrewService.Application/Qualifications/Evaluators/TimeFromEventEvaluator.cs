using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications.Evaluators;

public sealed class TimeFromEventEvaluator(
    IEmployeeRepository employeeRepository,
    ISeniorityRepository seniorityRepository,
    IEmployeeCertificationRepository certificationRepository) : IRequirementEvaluator
{
    public string Kind => RequirementKinds.TimeFromEvent;

    public async Task<EvaluationResult> EvaluateAsync(
        ControlNumber employeeCtrlNbr,
        QualificationRequirement rule,
        CancellationToken ct = default)
    {
        DateTime? eventDate = rule.EventSource switch
        {
            EventSources.EmploymentDate => await GetEmploymentDateAsync(employeeCtrlNbr, ct),
            EventSources.SeniorityDate => await GetSeniorityDateAsync(employeeCtrlNbr, ct),
            EventSources.CertificationDate => await GetLatestCertificationDateAsync(employeeCtrlNbr, ct),
            _ => null
        };

        if (eventDate is null)
            return EvaluationResult.NotSatisfied($"Could not resolve event source: {rule.EventSource}");

        var elapsed = rule.ThresholdUnit switch
        {
            ThresholdUnits.Days => (DateTime.UtcNow - eventDate.Value).TotalDays,
            ThresholdUnits.Months => (DateTime.UtcNow - eventDate.Value).TotalDays / 30.44,
            _ => 0
        };

        var met = elapsed >= rule.Threshold;
        var description = $"{(int)elapsed} {rule.ThresholdUnit.ToLowerInvariant()} since {rule.EventSource} ({eventDate.Value:yyyy-MM-dd})";

        return met
            ? EvaluationResult.Satisfied(description)
            : EvaluationResult.NotSatisfied($"{description} — need {rule.Threshold}");
    }

    private async Task<DateTime?> GetEmploymentDateAsync(ControlNumber employeeCtrlNbr, CancellationToken ct)
    {
        var employee = await employeeRepository.GetByCtrlNbrAsync(employeeCtrlNbr, ct);
        return employee?.EmploymentDate;
    }

    private async Task<DateTime?> GetSeniorityDateAsync(ControlNumber employeeCtrlNbr, CancellationToken ct)
    {
        _ = ct;
        var seniorityRecords = await seniorityRepository.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        var earliest = seniorityRecords
            .Where(s => s.LastActiveRoster)
            .MinBy(s => s.RosterDate);
        return earliest?.RosterDate;
    }

    private async Task<DateTime?> GetLatestCertificationDateAsync(ControlNumber employeeCtrlNbr, CancellationToken ct)
    {
        var certifications = await certificationRepository.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr, ct);
        var latest = certifications
            .Where(c => c.Status == CertificationStatuses.Active)
            .MaxBy(c => c.CertificationDate);
        return latest?.CertificationDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }
}
