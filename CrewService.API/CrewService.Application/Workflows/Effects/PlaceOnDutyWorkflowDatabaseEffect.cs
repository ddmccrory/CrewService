using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.Modules.Workflows;

namespace CrewService.Application.Workflows.Effects;

public sealed class PlaceOnDutyWorkflowDatabaseEffect : IDatabaseWorkflowEffect
{
    public string EffectTypeCode => WorkflowEffectTypeCodes.PlaceOnDuty;

    public async Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteAsync(WorkflowEffectExecutionContext context)
    {
        var payload = context.RuntimeContext.PlaceOnDutyPayload;
        if (payload is null)
            return [];

        var openRecords = await context.Uow.OnDutyRecords.GetOpenForEmployeeAsync(
            payload.EmployeeCtrlNbr,
            context.CancellationToken);

        var hasMatchingOpenRecord = openRecords.Any(r =>
            r.EmployeeCtrlNbr == payload.EmployeeCtrlNbr
            && r.PositionSlotCtrlNbr == payload.PositionSlotCtrlNbr
            && r.Status == OnDutyStatus.OnDuty);

        if (hasMatchingOpenRecord)
            return [];

        var lastOffDuty = await context.Uow.OffDutyRecords.GetLastForEmployeeAsync(
            payload.EmployeeCtrlNbr,
            context.CancellationToken);

        var previousRestHours = OnDutyHistoryCalculator.CalculatePreviousRestHours(lastOffDuty, payload.OnDutyTimeUtc);

        var recentOnDuty = await context.Uow.OnDutyRecords.GetRecentForEmployeeAsync(
            payload.EmployeeCtrlNbr,
            7,
            context.CancellationToken);

        var consecutiveDays = OnDutyHistoryCalculator.CalculateConsecutiveDays(recentOnDuty, payload.OnDutyTimeUtc);

        var record = OnDutyRecord.Create(
            payload.PositionSlotCtrlNbr,
            payload.EmployeeCtrlNbr,
            payload.OnDutyTimeUtc,
            payload.ScheduledOnDutyTimeUtc,
            previousRestHours,
            consecutiveDays,
            payload.IsAssigned,
            payload.LateCallThresholdMinutes);

        await context.Uow.OnDutyRecords.AddAsync(record, context.CancellationToken);

        return [];
    }
}
