namespace CrewService.Domain.Modules.Dispatching;

/// <summary>
/// Shared, side-effect-free calculations for on-duty record history. Callers load the relevant
/// off-duty/on-duty history from their unit of work and pass it in, so this logic is identical
/// whether a record is created manually (place on duty) or automatically (call-sheet generation).
/// </summary>
public static class OnDutyHistoryCalculator
{
    /// <summary>
    /// Sentinel previous-rest value used when an employee has no prior off-duty record (e.g. first
    /// ever tour). Large enough to always satisfy rest requirements.
    /// </summary>
    public const decimal NoPriorRestHours = 999m;

    /// <summary>
    /// Hours of rest between the employee's last off-duty time and the new on-duty time.
    /// Returns <see cref="NoPriorRestHours"/> when there is no prior off-duty record.
    /// </summary>
    public static decimal CalculatePreviousRestHours(OffDutyRecord? lastOffDuty, DateTime onDutyTimeUtc) =>
        lastOffDuty is null
            ? NoPriorRestHours
            : (decimal)(onDutyTimeUtc - lastOffDuty.OffDutyTimeUtc).TotalHours;

    /// <summary>
    /// Number of consecutive calendar days worked, counting the new on-duty day plus each prior day
    /// with an on-duty record and no gap. A gap greater than one day ends the streak.
    /// </summary>
    public static int CalculateConsecutiveDays(IReadOnlyList<OnDutyRecord> recentRecords, DateTime currentOnDutyUtc)
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
