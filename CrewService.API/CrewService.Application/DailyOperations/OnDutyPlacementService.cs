using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.DailyOperations;

public interface IOnDutyRecordRepository
{
    Task AddAsync(OnDutyRecord record, CancellationToken ct = default);
    Task<OnDutyRecord?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task<IReadOnlyList<OnDutyRecord>> GetRecentForEmployeeAsync(ControlNumber employeeCtrlNbr, int dayCount, CancellationToken ct = default);
}

public interface IOffDutyRecordRepository
{
    Task AddAsync(OffDutyRecord record, CancellationToken ct = default);
    Task<OffDutyRecord?> GetLastForEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
}

public sealed class OnDutyPlacementService(
    IShiftInstanceRepository shiftInstanceRepo,
    IOnDutyRecordRepository onDutyRepo,
    IOffDutyRecordRepository offDutyRepo)
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
        var lastOffDuty = await offDutyRepo.GetLastForEmployeeAsync(employeeCtrlNbr, ct);
        var previousRestHours = lastOffDuty is null
            ? 999m
            : (decimal)(onDutyTimeUtc - lastOffDuty.OffDutyTimeUtc).TotalHours;

        var recentOnDuty = await onDutyRepo.GetRecentForEmployeeAsync(employeeCtrlNbr, 7, ct);
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

        await onDutyRepo.AddAsync(record, ct);
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
