using CrewService.Domain.Modules.FraCompliance;

namespace CrewService.Application.FraCompliance;

public sealed class FraDutyTourCalculator
{
    /// <summary>
    /// Calculates Total Time On Duty (TTOD) from all segments within a duty tour.
    /// Covered service + commingled other service + deadhead-to-assignment = TTOD.
    /// Per §228.203(c)(1) and §228.11(b).
    /// </summary>
    public TtodResult Calculate(FraDutyTour tour)
    {
        var coveredMinutes = tour.Segments
            .Where(s => s.EndUtc.HasValue)
            .Sum(s => (int)(s.EndUtc!.Value - s.StartUtc).TotalMinutes);

        var commingledMinutes = tour.OtherServiceSegments
            .Where(s => s.IsCommingled)
            .Sum(s => (int)(s.EndUtc - s.StartUtc).TotalMinutes);

        var deadheadToAssignmentMinutes = tour.TransportationSegments
            .Where(s => s.IsToAssignment)
            .Sum(s => (int)(s.EndUtc - s.StartUtc).TotalMinutes);

        var deadheadFromAssignmentMinutes = tour.TransportationSegments
            .Where(s => !s.IsToAssignment)
            .Sum(s => (int)(s.EndUtc - s.StartUtc).TotalMinutes);

        var nonCommingledMinutes = tour.OtherServiceSegments
            .Where(s => !s.IsCommingled)
            .Sum(s => (int)(s.EndUtc - s.StartUtc).TotalMinutes);

        var totalTimeOnDuty = coveredMinutes + commingledMinutes + deadheadToAssignmentMinutes;

        return new TtodResult(
            CoveredServiceMinutes: coveredMinutes,
            CommingledMinutes: commingledMinutes,
            DeadheadToAssignmentMinutes: deadheadToAssignmentMinutes,
            DeadheadFromAssignmentMinutes: deadheadFromAssignmentMinutes,
            NonCommingledOtherMinutes: nonCommingledMinutes,
            TotalTimeOnDutyMinutes: totalTimeOnDuty);
    }

    /// <summary>
    /// Calculates prior time off from the previous tour's end to this tour's start.
    /// </summary>
    public int CalculatePriorTimeOffMinutes(DateTime? previousTourEndUtc, DateTime currentTourStartUtc)
    {
        if (previousTourEndUtc is null)
            return int.MaxValue; // No prior tour = unlimited rest

        return (int)(currentTourStartUtc - previousTourEndUtc.Value).TotalMinutes;
    }
}

public sealed record TtodResult(
    int CoveredServiceMinutes,
    int CommingledMinutes,
    int DeadheadToAssignmentMinutes,
    int DeadheadFromAssignmentMinutes,
    int NonCommingledOtherMinutes,
    int TotalTimeOnDutyMinutes);
