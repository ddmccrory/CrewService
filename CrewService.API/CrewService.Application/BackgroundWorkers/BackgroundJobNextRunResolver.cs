using CrewService.Application.Bulletins;
using CrewService.Application.DailyOperations;
using CrewService.Application.Policies;
using CrewService.Application.SeniorityOps;
using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.BackgroundWorkers;

public sealed record BackgroundJobNextRunResult(
    DateTime NextUtc,
    string? ShiftCode = null,
    string? ShiftDisplayName = null,
    DateOnly? TargetDate = null,
    string? DepartmentName = null);

public interface IBackgroundJobNextRunResolver
{
    Task<BackgroundJobNextRunResult?> ResolveAsync(
        string workerType,
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber owningRailroadCtrlNbr,
        CancellationToken ct = default);
}

public sealed class BackgroundJobNextRunResolver(
    IDailyCallSheetSchedulerService callSheetScheduler,
    IDailyCallSheetManualOverrideStore callSheetManualOverrides,
    BulletinsService bulletinsService,
    PoliciesService policiesService,
    SeniorityAppService seniorityService,
    IOrchestrationUnitOfWorkFactory uowFactory)
    : IBackgroundJobNextRunResolver
{
    public async Task<BackgroundJobNextRunResult?> ResolveAsync(
        string workerType,
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber owningRailroadCtrlNbr,
        CancellationToken ct = default)
    {
        if (workerType.Equals("CallSheet", StringComparison.OrdinalIgnoreCase))
            return await ResolveCallSheetNextRunAsync(workAreaGroupCtrlNbr, ct);

        if (workerType.Equals("Bulletin", StringComparison.OrdinalIgnoreCase))
        {
            var (nextUtc, workAreaCtrlNbr) = await bulletinsService.GetNextBulletinEventAsync(ct);
            if (!nextUtc.HasValue || workAreaCtrlNbr != workAreaGroupCtrlNbr.Value)
                return null;

            return new BackgroundJobNextRunResult(DateTime.SpecifyKind(nextUtc.Value, DateTimeKind.Utc));
        }

        if (workerType.Equals("SeniorityMove", StringComparison.OrdinalIgnoreCase))
        {
            var nextUtc = await policiesService.GetNextActiveSeniorityMoveEffectiveUtcForRailroadAsync(
                owningRailroadCtrlNbr,
                ct);
            return nextUtc.HasValue
                ? new BackgroundJobNextRunResult(DateTime.SpecifyKind(nextUtc.Value, DateTimeKind.Utc))
                : null;
        }

        if (workerType.Equals("SeniorityStateChange", StringComparison.OrdinalIgnoreCase))
        {
            var (nextUtc, nextWorkAreaCtrlNbr, _) = await seniorityService.GetNextPendingChangeForRailroadAsync(
                owningRailroadCtrlNbr,
                ct);
            if (!nextUtc.HasValue || nextWorkAreaCtrlNbr != workAreaGroupCtrlNbr)
                return null;

            return new BackgroundJobNextRunResult(DateTime.SpecifyKind(nextUtc.Value, DateTimeKind.Utc));
        }

        if (workerType.Equals("MarkOff", StringComparison.OrdinalIgnoreCase))
        {
            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
            var nextUtc = await uow.AbsenceRequests.GetNextApprovedAutoMarkOffStartUtcAsync(ct);
            return nextUtc.HasValue
                ? new BackgroundJobNextRunResult(DateTime.SpecifyKind(nextUtc.Value, DateTimeKind.Utc))
                : null;
        }

        return null;
    }

    private async Task<BackgroundJobNextRunResult?> ResolveCallSheetNextRunAsync(
        ControlNumber workAreaGroupCtrlNbr,
        CancellationToken ct)
    {
        var schedulerCandidate = await callSheetScheduler.GetNextCallSheetEventCandidateAsync(workAreaGroupCtrlNbr, ct);
        var manualCandidate = callSheetManualOverrides.GetNextScheduled(workAreaGroupCtrlNbr);

        if (schedulerCandidate is null && manualCandidate is null)
            return null;

        if (manualCandidate is not null
            && (schedulerCandidate is null || manualCandidate.Value.ScheduledUtc < schedulerCandidate.EventUtc))
        {
            string shiftCode = string.Empty;
            string shiftDisplayName = string.Empty;
            string departmentName = string.Empty;

            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
            var shiftDef = await uow.ShiftDefinitions.GetByCtrlNbrAsync(
                manualCandidate.Value.Item.ShiftDefinitionCtrlNbr,
                ct);
            if (shiftDef is not null)
            {
                shiftCode = shiftDef.ShiftCode;
                shiftDisplayName = shiftDef.DisplayName;
            }

            if (manualCandidate.Value.Item.DepartmentCtrlNbr is { } deptCtrlNbr)
            {
                var dept = await uow.Departments.GetByCtrlNbrAsync(deptCtrlNbr, ct);
                departmentName = dept?.Name ?? string.Empty;
            }

            return new BackgroundJobNextRunResult(
                DateTime.SpecifyKind(manualCandidate.Value.ScheduledUtc, DateTimeKind.Utc),
                shiftCode,
                shiftDisplayName,
                manualCandidate.Value.Item.TargetDate,
                departmentName);
        }

        var next = schedulerCandidate!;
        return new BackgroundJobNextRunResult(
            DateTime.SpecifyKind(next.EventUtc, DateTimeKind.Utc),
            next.ShiftCode,
            next.ShiftDisplayName,
            next.Item.TargetDate,
            next.DepartmentName);
    }
}
