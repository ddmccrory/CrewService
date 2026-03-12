using CrewService.Domain.Modules.FraCompliance;

namespace CrewService.Application.FraCompliance;

/// <summary>
/// Handles the impact of a D&A violation on certification eligibility.
/// Ineligibility periods per §240.119(e) / §242.115:
///   1st violation: during evaluation + primary treatment
///   2nd violation (within 60 months): 2 years
///   3rd+ violation (within 60 months): permanent
/// Refusal to test is treated as a single violation.
/// </summary>
public sealed class DrugAlcoholCertificationImpactHandler
{
    /// <summary>
    /// Determines the ineligibility period based on violation history.
    /// </summary>
    public IneligibilityResult DetermineIneligibility(
        DrugAlcoholTestRecord currentViolation,
        IReadOnlyList<DrugAlcoholTestRecord> priorViolations)
    {
        if (!currentViolation.IsViolation)
            return new IneligibilityResult(IsIneligible: false, PeriodMonths: null, IsPermanent: false, ViolationCount: 0);

        var sixtyMonthsAgo = currentViolation.TestDate.AddMonths(-60);

        var recentViolationCount = priorViolations
            .Count(v => v.IsViolation && v.TestDate >= sixtyMonthsAgo);

        var totalCount = recentViolationCount + 1; // include current

        return totalCount switch
        {
            1 => new IneligibilityResult(
                IsIneligible: true,
                PeriodMonths: null, // during evaluation + treatment (variable)
                IsPermanent: false,
                ViolationCount: 1),

            2 => new IneligibilityResult(
                IsIneligible: true,
                PeriodMonths: 24,
                IsPermanent: false,
                ViolationCount: 2),

            _ => new IneligibilityResult(
                IsIneligible: true,
                PeriodMonths: null,
                IsPermanent: true,
                ViolationCount: totalCount)
        };
    }

    /// <summary>
    /// Determines whether a cross-revocation should occur.
    /// Per §242.213(h): conductor cert revocation for signal violations also revokes engineer cert.
    /// </summary>
    public bool ShouldCrossRevoke(string violationType)
    {
        var crossRevocationViolations = new[]
        {
            "242.403(e)(1)", "242.403(e)(2)", "242.403(e)(3)",
            "242.403(e)(4)", "242.403(e)(5)", "242.403(e)(12)"
        };

        return crossRevocationViolations.Contains(violationType);
    }
}

public sealed record IneligibilityResult(
    bool IsIneligible,
    int? PeriodMonths,
    bool IsPermanent,
    int ViolationCount);
