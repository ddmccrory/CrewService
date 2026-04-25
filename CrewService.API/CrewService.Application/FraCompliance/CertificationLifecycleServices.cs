using CrewService.Domain.Modules.FraCompliance;

namespace CrewService.Application.FraCompliance;

/// <summary>
/// Provides staleness limit lookups for use when recording new eligibility checks.
/// Status derivation lives in EmployeeCertification.RecomputeStatus().
/// </summary>
public sealed class CertificationEligibilityService
{
    public int GetStalenessLimitDays(string checkType)
        => EligibilityCheckStalenessLimits.Get(checkType);
}

/// <summary>
/// Flags certifications expiring within the configured interval.
/// </summary>
public sealed class CertificationExpirationService
{
    public bool IsExpired(EmployeeCertification certification, DateOnly asOfDate)
        => asOfDate >= certification.ExpirationDate;

    public bool IsExpiringSoon(EmployeeCertification certification, DateOnly asOfDate, int warningDays = 90)
        => asOfDate >= certification.ExpirationDate.AddDays(-warningDays) && !IsExpired(certification, asOfDate);
}
