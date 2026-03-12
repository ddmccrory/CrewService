using CrewService.Domain.Modules.FraCompliance;

namespace CrewService.Application.FraCompliance;

public sealed class FraRestValidator
{
    /// <summary>
    /// Calculates required rest hours after a duty tour.
    /// Base 10h + penalty rest for each hour (or fraction) beyond 12h max.
    /// Per §228.11(b)(12).
    /// </summary>
    public RestRequirement CalculateRestRequirement(RegulatoryStandard standard, int totalTimeOnDutyMinutes)
    {
        var baseRestMinutes = standard.MinRestMinutes;
        var excessMinutes = Math.Max(0, totalTimeOnDutyMinutes - standard.MaxOnDutyMinutes);
        var penaltyMinutes = excessMinutes > 0 ? (int)Math.Ceiling(excessMinutes / 60.0) * 60 : 0;
        var totalRestMinutes = baseRestMinutes + penaltyMinutes;

        return new RestRequirement(
            BaseRestMinutes: baseRestMinutes,
            PenaltyMinutes: penaltyMinutes,
            TotalRestMinutes: totalRestMinutes,
            ExcessMinutes: excessMinutes);
    }

    /// <summary>
    /// Validates the 10h minimum post-tour rest requirement.
    /// Returns false if the employee would go on duty before rest is complete.
    /// </summary>
    public bool ValidatePostTourRest(int actualRestMinutes, int requiredRestMinutes)
    {
        return actualRestMinutes >= requiredRestMinutes;
    }

    /// <summary>
    /// Validates the 8h rest in preceding 24h requirement (train employees only).
    /// Per §228.11(b)(12)(iii).
    /// </summary>
    public bool Validate8hInPreceding24h(
        RegulatoryStandard standard,
        DateTime proposedOnDutyUtc,
        IReadOnlyList<(DateTime Start, DateTime End)> recentDutyPeriods)
    {
        if (!standard.Min8hRestInPreceding24h)
            return true;

        var windowStart = proposedOnDutyUtc.AddHours(-24);
        var totalOnDutyInWindow = recentDutyPeriods
            .Select(p =>
            {
                var effectiveStart = p.Start < windowStart ? windowStart : p.Start;
                var effectiveEnd = p.End > proposedOnDutyUtc ? proposedOnDutyUtc : p.End;
                return Math.Max(0, (effectiveEnd - effectiveStart).TotalMinutes);
            })
            .Sum();

        var totalOffDutyInWindow = (24 * 60) - totalOnDutyInWindow;
        return totalOffDutyInWindow >= 480; // 8h = 480 min
    }

    /// <summary>
    /// Detects quick tie-up: within 3 minutes of max on-duty time.
    /// Per §228.203(c)(6).
    /// </summary>
    public bool IsQuickTieUp(RegulatoryStandard standard, int totalTimeOnDutyMinutes)
    {
        return totalTimeOnDutyMinutes >= (standard.MaxOnDutyMinutes - 3);
    }
}

public sealed record RestRequirement(
    int BaseRestMinutes,
    int PenaltyMinutes,
    int TotalRestMinutes,
    int ExcessMinutes);
