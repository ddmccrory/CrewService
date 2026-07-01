using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public sealed class OnDutyPlacementService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<OnDutyRecord> ExecuteAsync(
        ControlNumber positionSlotCtrlNbr,
        ControlNumber employeeCtrlNbr,
        DateTime onDutyTimeUtc,
        DateTime scheduledOnDutyTimeUtc,
        bool isAssigned,
        int lateCallThresholdMinutes = 0,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var lastOffDuty = await uow.OffDutyRecords.GetLastForEmployeeAsync(employeeCtrlNbr, ct);
        var previousRestHours = OnDutyHistoryCalculator.CalculatePreviousRestHours(lastOffDuty, onDutyTimeUtc);

        var recentOnDuty = await uow.OnDutyRecords.GetRecentForEmployeeAsync(employeeCtrlNbr, 7, ct);
        var consecutiveDays = OnDutyHistoryCalculator.CalculateConsecutiveDays(recentOnDuty, onDutyTimeUtc);

        var record = OnDutyRecord.Create(
            positionSlotCtrlNbr,
            employeeCtrlNbr,
            onDutyTimeUtc,
            scheduledOnDutyTimeUtc,
            previousRestHours,
            consecutiveDays,
            isAssigned,
            lateCallThresholdMinutes);

        await uow.OnDutyRecords.AddAsync(record, ct);
        await uow.CommitAsync(ct);
        return record;
    }
}

