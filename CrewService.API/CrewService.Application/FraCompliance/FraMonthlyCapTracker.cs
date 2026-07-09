using CrewService.Domain.Modules.FraCompliance;

namespace CrewService.Application.FraCompliance;

public sealed class FraMonthlyCapTracker
{
    private readonly byte _instanceSentinel = 0;

    /// <summary>
    /// Updates the monthly accumulator with minutes from a completed tour
    /// and checks whether any monthly cap has been exceeded.
    /// Per §228.11(b)(14-15).
    /// </summary>
    public MonthlyCapResult UpdateAndCheck(
        RegulatoryStandard standard,
        FraMonthlyAccumulator accumulator,
        TtodResult ttodResult)
    {
        _ = _instanceSentinel;
        accumulator.AddTourMinutes(
            coveredServiceMinutes: ttodResult.CoveredServiceMinutes,
            deadheadToReleaseMinutes: ttodResult.DeadheadFromAssignmentMinutes,
            otherServiceMinutes: ttodResult.NonCommingledOtherMinutes,
            deadheadAfter12hMinutes: CalculateDeadheadAfter12h(standard, ttodResult));

        var totalExceeded = accumulator.TotalMinutes > standard.MonthlyCapMinutes;
        var deadheadExceeded = accumulator.DeadheadAfter12hMinutes > standard.DeadheadAfter12hMonthlyCapMinutes;

        return new MonthlyCapResult(
            TotalMinutes: accumulator.TotalMinutes,
            DeadheadAfter12hMinutes: accumulator.DeadheadAfter12hMinutes,
            MonthlyCapExceeded: totalExceeded,
            DeadheadCapExceeded: deadheadExceeded,
            MonthlyCapLimitMinutes: standard.MonthlyCapMinutes,
            DeadheadCapLimitMinutes: standard.DeadheadAfter12hMonthlyCapMinutes);
    }

    private static int CalculateDeadheadAfter12h(RegulatoryStandard standard, TtodResult ttod)
    {
        if (ttod.TotalTimeOnDutyMinutes <= standard.MaxOnDutyMinutes)
            return 0;

        return ttod.DeadheadFromAssignmentMinutes;
    }
}

public sealed record MonthlyCapResult(
    int TotalMinutes,
    int DeadheadAfter12hMinutes,
    bool MonthlyCapExceeded,
    bool DeadheadCapExceeded,
    int MonthlyCapLimitMinutes,
    int DeadheadCapLimitMinutes);
