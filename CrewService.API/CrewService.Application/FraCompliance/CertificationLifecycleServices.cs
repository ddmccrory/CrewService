using CrewService.Domain.Modules.FraCompliance;

namespace CrewService.Application.FraCompliance;

/// <summary>
/// Validates all 7 eligibility check types and enforces staleness limits (§240.217).
/// </summary>
public sealed class CertificationEligibilityService
{
    private static readonly Dictionary<string, int> StalenessLimits = new()
    {
        ["SafetyConduct"] = 366,
        ["MotorVehicle"] = 366,
        ["SubstanceAbuse"] = 366,
        ["Vision"] = 450,
        ["Hearing"] = 450,
        ["Knowledge"] = 366,
        ["Performance"] = 366
    };

    public bool AreAllChecksValid(EmployeeCertification certification, DateOnly asOfDate)
    {
        var requiredTypes = StalenessLimits.Keys;

        foreach (var checkType in requiredTypes)
        {
            var check = certification.EligibilityChecks
                .Where(c => c.CheckType == checkType && c.Result == "Pass")
                .OrderByDescending(c => c.EvaluationDate)
                .FirstOrDefault();

            if (check is null || check.IsStale(asOfDate))
                return false;
        }

        return true;
    }

    public IReadOnlyList<string> GetStaleOrMissingChecks(EmployeeCertification certification, DateOnly asOfDate)
    {
        var missing = new List<string>();

        foreach (var (checkType, _) in StalenessLimits)
        {
            var check = certification.EligibilityChecks
                .Where(c => c.CheckType == checkType && c.Result == "Pass")
                .OrderByDescending(c => c.EvaluationDate)
                .FirstOrDefault();

            if (check is null || check.IsStale(asOfDate))
                missing.Add(checkType);
        }

        return missing;
    }

    public int GetStalenessLimitDays(string checkType)
        => StalenessLimits.TryGetValue(checkType, out var days) ? days : 366;
}

/// <summary>
/// Flags certifications expiring within the 36-month interval.
/// </summary>
public sealed class CertificationExpirationService
{
    public bool IsExpired(EmployeeCertification certification, DateOnly asOfDate)
        => asOfDate >= certification.ExpirationDate;

    public bool IsExpiringSoon(EmployeeCertification certification, DateOnly asOfDate, int warningDays = 90)
        => asOfDate >= certification.ExpirationDate.AddDays(-warningDays) && !IsExpired(certification, asOfDate);
}

/// <summary>
/// Tracks annual monitoring observation and compliance test requirements (§240.303).
/// </summary>
public sealed class CertificationMonitoringService
{
    public bool IsMonitoringObservationCurrent(EmployeeCertification certification)
    {
        if (certification.LastMonitoringObservationUtc is null)
            return false;

        var oneYearAgo = DateTime.UtcNow.AddYears(-1);
        return certification.LastMonitoringObservationUtc.Value >= oneYearAgo;
    }

    public bool IsComplianceTestCurrent(EmployeeCertification certification)
    {
        if (certification.LastComplianceTestUtc is null)
            return false;

        var oneYearAgo = DateTime.UtcNow.AddYears(-1);
        return certification.LastComplianceTestUtc.Value >= oneYearAgo;
    }

    public bool IsFullyCompliant(EmployeeCertification certification)
        => IsMonitoringObservationCurrent(certification) && IsComplianceTestCurrent(certification);
}
