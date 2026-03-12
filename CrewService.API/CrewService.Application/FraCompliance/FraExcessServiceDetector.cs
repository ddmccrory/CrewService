using CrewService.Domain.Modules.FraCompliance;

namespace CrewService.Application.FraCompliance;

/// <summary>
/// Evaluates all 10 violation types per §228.19(b)(1)–(10) on tour close.
/// </summary>
public sealed class FraExcessServiceDetector
{
    private readonly FraRestValidator _restValidator = new();

    public IReadOnlyList<string> DetectViolations(
        RegulatoryStandard standard,
        FraDutyTour tour,
        TtodResult ttod,
        MonthlyCapResult? monthlyCap,
        ConsecutiveDayResult? consecutiveDay,
        int priorRestMinutes,
        IReadOnlyList<(DateTime Start, DateTime End)> recentDutyPeriods)
    {
        var violations = new List<string>();

        // (1) Exceeded maximum on-duty time
        if (ttod.TotalTimeOnDutyMinutes > standard.MaxOnDutyMinutes)
            violations.Add("ExceededMaxOnDuty");

        // (2) Insufficient rest before going on duty
        if (priorRestMinutes < standard.MinRestMinutes)
            violations.Add("InsufficientPriorRest");

        // (3) 8h rest in preceding 24h not met (train employees)
        if (standard.Min8hRestInPreceding24h && tour.DutyTourStartUtc != default)
        {
            if (!_restValidator.Validate8hInPreceding24h(standard, tour.DutyTourStartUtc, recentDutyPeriods))
                violations.Add("Insufficient8hInPreceding24h");
        }

        // (4) Exceeded consecutive day limit — 6 days
        if (consecutiveDay is { LimitReached: true, Tier: 6 })
            violations.Add("ExceededConsecutive6Days");

        // (5) Exceeded consecutive day limit — 7 days
        if (consecutiveDay is { LimitReached: true, Tier: 7 })
            violations.Add("ExceededConsecutive7Days");

        // (6) Exceeded monthly 276h cap
        if (monthlyCap is { MonthlyCapExceeded: true })
            violations.Add("ExceededMonthlyCap");

        // (7) Exceeded monthly 30h deadhead-after-12h cap
        if (monthlyCap is { DeadheadCapExceeded: true })
            violations.Add("ExceededDeadheadMonthlyCap");

        // (8) Wreck/relief service beyond 4h extra allowance
        var wreckLimit = standard.MaxOnDutyMinutes + standard.WreckReliefExtraMinutes;
        if (ttod.TotalTimeOnDutyMinutes > wreckLimit)
            violations.Add("ExceededWreckReliefLimit");

        // (9) Commingled service exceeded limits
        if (ttod.CommingledMinutes > 0 && ttod.TotalTimeOnDutyMinutes > standard.MaxOnDutyMinutes)
            violations.Add("CommingledServiceExcess");

        // (10) Quick tie-up with excess service
        if (tour.IsQuickTieUp && ttod.TotalTimeOnDutyMinutes > standard.MaxOnDutyMinutes)
            violations.Add("QuickTieUpWithExcess");

        return violations;
    }
}
