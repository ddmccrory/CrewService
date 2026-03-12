using CrewService.Domain.Modules.FraCompliance;

namespace CrewService.Application.FraCompliance;

public sealed class FraConsecutiveDayTracker
{
    /// <summary>
    /// Calculates consecutive days with on-duty initiated.
    /// A day counts if on-duty was initiated; rest ≥ 24h resets the counter.
    /// Per §228.11(b)(16).
    /// </summary>
    public int CalculateConsecutiveDays(
        IReadOnlyList<DateTime> recentOnDutyStartDatesUtc,
        DateTime currentOnDutyStartUtc)
    {
        if (recentOnDutyStartDatesUtc.Count == 0)
            return 1;

        var sorted = recentOnDutyStartDatesUtc
            .OrderByDescending(d => d)
            .ToList();

        var consecutiveDays = 1;
        var currentDate = currentOnDutyStartUtc.Date;

        foreach (var previousStart in sorted)
        {
            var previousDate = previousStart.Date;
            var gap = (currentDate - previousDate).TotalDays;

            if (gap <= 1)
            {
                if (previousDate != currentDate)
                    consecutiveDays++;
                currentDate = previousDate;
            }
            else
            {
                break;
            }
        }

        return consecutiveDays;
    }

    /// <summary>
    /// Evaluates whether a consecutive day limit has been reached and returns
    /// the required rest. Two-tier: 6 days → 48h, 7 days → 72h.
    /// Per §228.11(b)(16).
    /// </summary>
    public ConsecutiveDayResult Evaluate(
        RegulatoryStandard standard,
        int consecutiveDays,
        bool isAtHomeTerminal)
    {
        if (consecutiveDays >= standard.ConsecutiveDayLimit7)
        {
            return new ConsecutiveDayResult(
                LimitReached: true,
                RequiredRestMinutes: standard.RestAfter7DaysMinutes,
                Tier: 7,
                RequiresHomeTerminal: true);
        }

        if (consecutiveDays >= standard.ConsecutiveDayLimit6 && isAtHomeTerminal)
        {
            return new ConsecutiveDayResult(
                LimitReached: true,
                RequiredRestMinutes: standard.RestAfter6DaysMinutes,
                Tier: 6,
                RequiresHomeTerminal: true);
        }

        return new ConsecutiveDayResult(
            LimitReached: false,
            RequiredRestMinutes: 0,
            Tier: 0,
            RequiresHomeTerminal: false);
    }
}

public sealed record ConsecutiveDayResult(
    bool LimitReached,
    int RequiredRestMinutes,
    int Tier,
    bool RequiresHomeTerminal);
