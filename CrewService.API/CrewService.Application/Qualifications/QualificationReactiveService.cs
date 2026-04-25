using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Qualifications;

public sealed class QualificationReactiveService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    RequirementEvaluationService requirementEvaluationService)
{
    public async Task HandleAddedToRosterAsync(ControlNumber employeeCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var qualificationTypes = await uow.QualificationTypes.GetActiveByCraftCtrlNbrAsync(craftCtrlNbr);

        foreach (var qualificationType in qualificationTypes.Where(q => q.Requirements.Count > 0))
            await requirementEvaluationService.EvaluateAsync(employeeCtrlNbr, qualificationType, uow, ct);

        await uow.CommitAsync(ct);
    }

    public async Task HandleOnDutyRecordCreatedAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var qualificationTypes = await uow.QualificationTypes.GetAllAsync(ct);

        foreach (var qualificationType in qualificationTypes.Where(q =>
                     q.IsActive &&
                     q.Requirements.Count > 0 &&
                     string.Equals(q.EvaluationStrategy, EvaluationStrategies.ActivityCount, StringComparison.OrdinalIgnoreCase)))
        {
            await requirementEvaluationService.EvaluateAsync(employeeCtrlNbr, qualificationType, uow, ct);
        }

        await uow.CommitAsync(ct);
    }
}

