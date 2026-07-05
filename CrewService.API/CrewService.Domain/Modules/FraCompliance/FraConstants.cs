namespace CrewService.Domain.Modules.FraCompliance;

public static class CertificationStatuses
{
    public const string Pending   = "Pending";
    public const string Active    = "Active";
    public const string Renew     = "Renew";
    public const string Suspended = "Suspended";
    public const string Revoked   = "Revoked";
    public const string Expired   = "Expired";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// Default check-type configurations used for seeding and fallback.
/// Tuple: (CheckType, DisplayName, StalenessLimitDays, IsEnforced, IsEnforcementLocked)
/// </summary>
public static class CertificationCheckDefaults
{
    public static readonly IReadOnlyList<(string CheckType, string DisplayName, int StalenessLimitDays, bool IsEnforced, bool IsEnforcementLocked)> Checks =
    [
        ("SafetyConduct",         "Safety Conduct",         366, true,  false),
        ("MotorVehicle",          "Motor Vehicle",          366, true,  false),
        ("SubstanceAbuse",        "Substance Abuse",        366, true,  false),
        ("Vision",                "Vision",                 450, true,  false),
        ("Hearing",               "Hearing",                450, true,  false),
        ("Knowledge",             "Knowledge",              366, true,  false),
        ("Performance",           "Performance",            366, true,  false),
        ("OperationalMonitoring", "Operational Monitoring", 366, true,  true),
        ("ComplianceTest",        "Compliance Test",        366, true,  true),
    ];

    public static string GetDisplayName(string checkType)
    {
        foreach (var (ct, displayName, _, _, _) in Checks)
            if (string.Equals(ct, checkType, StringComparison.OrdinalIgnoreCase))
                return displayName;
        return checkType;
    }

    public static int GetStalenessLimitDays(string checkType)
    {
        foreach (var (ct, _, days, _, _) in Checks)
            if (string.Equals(ct, checkType, StringComparison.OrdinalIgnoreCase))
                return days;
        return 366;
    }
}

/// <summary>
/// §240.217 staleness limits (in days) keyed by check type (case-insensitive).
/// Kept for backward compatibility; prefer FraCertificationCheckConfig from the database.
/// </summary>
public static class EligibilityCheckStalenessLimits
{
    public static readonly IReadOnlyDictionary<string, int> Days =
        CertificationCheckDefaults.Checks
            .ToDictionary(c => c.CheckType, c => c.StalenessLimitDays, StringComparer.OrdinalIgnoreCase);

    public static int Get(string checkType)
        => Days.TryGetValue(checkType, out var d) ? d : 366;
}
