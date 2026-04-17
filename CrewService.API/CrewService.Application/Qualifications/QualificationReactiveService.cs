using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

public sealed class QualificationReactiveService(
    IQualificationTypeRepository qualificationTypeRepository,
    PrerequisiteEvaluationService prerequisiteEvaluationService)
{
    public async Task HandleOnDutyRecordCreatedAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        var qualificationTypes = await qualificationTypeRepository.GetAllAsync(ct);

        foreach (var qualificationType in qualificationTypes.Where(q =>
                     q.IsActive &&
                     string.Equals(q.EvaluationStrategy, "ActivityCount", StringComparison.OrdinalIgnoreCase)))
        {
            await prerequisiteEvaluationService.EvaluateAsync(employeeCtrlNbr, qualificationType, ct);
        }
    }

    public async Task HandleEmployeeCreatedAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        var qualificationTypes = await qualificationTypeRepository.GetAllAsync(ct);

        foreach (var qualificationType in qualificationTypes.Where(q => q.IsActive))
        {
            await prerequisiteEvaluationService.EvaluateAsync(employeeCtrlNbr, qualificationType, ct);
        }
    }
}
