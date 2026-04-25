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
        var previousRestHours = lastOffDuty is null
            ? 999m
            : (decimal)(onDutyTimeUtc - lastOffDuty.OffDutyTimeUtc).TotalHours;

        var recentOnDuty = await uow.OnDutyRecords.GetRecentForEmployeeAsync(employeeCtrlNbr, 7, ct);
        var consecutiveDays = CalculateConsecutiveDays(recentOnDuty, onDutyTimeUtc);

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

    private static int CalculateConsecutiveDays(IReadOnlyList<OnDutyRecord> recentRecords, DateTime currentOnDutyUtc)
    {
        if (recentRecords.Count == 0) return 1;

        var count = 1;
        var currentDate = currentOnDutyUtc.Date;

        foreach (var rec in recentRecords.OrderByDescending(r => r.OnDutyTimeUtc))
        {
            var recDate = rec.OnDutyTimeUtc.Date;
            var gap = (currentDate - recDate).TotalDays;
            if (gap <= 1 && recDate != currentDate)
            {
                count++;
                currentDate = recDate;
            }
            else if (gap > 1) break;
        }

        return count;
    }
}

