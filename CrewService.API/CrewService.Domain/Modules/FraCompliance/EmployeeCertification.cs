using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class EmployeeCertification : Entity
{
    private readonly List<CertificationEligibilityCheck> _eligibilityChecks = [];

    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber RegulatoryQualificationCtrlNbr { get; private set; }
    public string CertificationType { get; private set; } = string.Empty;
    public DateOnly CertificationDate { get; private set; }
    public DateOnly ExpirationDate { get; private set; }
    public string Status { get; private set; } = CertificationStatuses.Pending;
    public string? CertificationNumber { get; private set; }
    public DateTime? SuspendedAtUtc { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTime? RevocationPeriodEndUtc { get; private set; }

    public IReadOnlyList<CertificationEligibilityCheck> EligibilityChecks => _eligibilityChecks.AsReadOnly();

    private EmployeeCertification()
    {
        EmployeeCtrlNbr = null!;
        RegulatoryQualificationCtrlNbr = null!;
    }

    public static EmployeeCertification Create(
        ControlNumber employeeCtrlNbr,
        ControlNumber regulatoryQualificationCtrlNbr,
        string certificationType,
        DateOnly certificationDate,
        int recertificationIntervalMonths,
        string? certificationNumber = null)
    {
        return new EmployeeCertification
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            RegulatoryQualificationCtrlNbr = regulatoryQualificationCtrlNbr,
            CertificationType = certificationType,
            CertificationDate = certificationDate,
            ExpirationDate = certificationDate.AddMonths(recertificationIntervalMonths),
            Status = CertificationStatuses.Pending,
            CertificationNumber = certificationNumber
        };
    }

    /// <summary>
    /// Derives status purely from data. Call after any check mutation or on the daily worker sweep.
    /// Suspended and Revoked are administrative overrides and are never cleared by this method.
    /// Pass checkConfigs to use railroad-configured staleness limits and enforcement rules;
    /// falls back to built-in defaults when null.
    /// </summary>
    public void RecomputeStatus(DateOnly asOfDate, IReadOnlyList<FraCertificationCheckConfig>? checkConfigs = null)
    {
        if (Status is CertificationStatuses.Suspended or CertificationStatuses.Revoked)
            return;

        if (asOfDate >= ExpirationDate)
        {
            Status = CertificationStatuses.Expired;
            return;
        }

        // Build effective check rules: use configs when provided, else fall back to built-in defaults.
        var enforcedChecks = checkConfigs is not null
            ? checkConfigs
                .Where(c => c.IsEnforced)
                .Select(c => (c.CheckType, c.StalenessLimitDays))
                .ToList()
            : CertificationCheckDefaults.Checks
                .Where(c => c.IsEnforced)
                .Select(c => (c.CheckType, c.StalenessLimitDays))
                .ToList();

        var missingOrStale = enforcedChecks
            .Where(rule => !_eligibilityChecks
                .Any(c => string.Equals(c.CheckType, rule.CheckType, StringComparison.OrdinalIgnoreCase)
                       && c.Result == "Pass"
                       && !c.IsStale(asOfDate)))
            .ToList();

        if (missingOrStale.Count > 0)
        {
            // Renew = approaching stale (within RenewWindowDays) OR all types have at least a stale pass
            // Pending = at least one enforced type has never passed at all.
            Status = missingOrStale.All(rule =>
                _eligibilityChecks.Any(c =>
                    string.Equals(c.CheckType, rule.CheckType, StringComparison.OrdinalIgnoreCase) && c.Result == "Pass"))
                ? CertificationStatuses.Renew
                : CertificationStatuses.Pending;
            return;
        }

        Status = CertificationStatuses.Active;

        // Auto-generate certification number on first Active transition.
        if (string.IsNullOrWhiteSpace(CertificationNumber))
            CertificationNumber = GenerateCertificationNumber(asOfDate);
    }

    private string GenerateCertificationNumber(DateOnly asOfDate)
        => $"{CertificationType.ToUpperInvariant()}-{asOfDate:yyyyMMdd}-{CtrlNbr.Value}";

    public void UpdateCertificationDetails(
        ControlNumber regulatoryQualificationCtrlNbr,
        string certificationType,
        DateOnly certificationDate,
        int recertificationIntervalMonths,
        string? certificationNumber)
    {
        RegulatoryQualificationCtrlNbr = regulatoryQualificationCtrlNbr;
        CertificationType = certificationType;
        CertificationDate = certificationDate;
        ExpirationDate = certificationDate.AddMonths(recertificationIntervalMonths);
        CertificationNumber = certificationNumber;
    }

    public void Suspend(string reason)
    {
        Status = CertificationStatuses.Suspended;
        SuspendedAtUtc = DateTime.UtcNow;
        SuspensionReason = reason;
    }

    public void Revoke(DateTime revocationPeriodEndUtc)
    {
        Status = CertificationStatuses.Revoked;
        RevocationPeriodEndUtc = revocationPeriodEndUtc;
    }

    public void Reinstate(DateOnly asOfDate)
    {
        SuspendedAtUtc = null;
        SuspensionReason = null;
        RevocationPeriodEndUtc = null;
        Status = CertificationStatuses.Pending;
        RecomputeStatus(asOfDate);
    }

    public CertificationEligibilityCheck AddEligibilityCheck(
        string checkType, DateOnly evaluationDate,
        int stalenessLimitDays, string result, string? evaluatorName)
    {
        var check = CertificationEligibilityCheck.Create(
            CtrlNbr, checkType, evaluationDate, stalenessLimitDays, result, evaluatorName);
        _eligibilityChecks.Add(check);
        RecomputeStatus(DateOnly.FromDateTime(DateTime.UtcNow));
        return check;
    }

    public CertificationEligibilityCheck UpdateEligibilityCheck(
        ControlNumber eligibilityCheckCtrlNbr,
        string checkType,
        DateOnly evaluationDate,
        int stalenessLimitDays,
        string result,
        string? evaluatorName)
    {
        var check = _eligibilityChecks.FirstOrDefault(c => c.CtrlNbr == eligibilityCheckCtrlNbr)
            ?? throw new InvalidOperationException("Eligibility check not found");

        check.Update(checkType, evaluationDate, stalenessLimitDays, result, evaluatorName);
        RecomputeStatus(DateOnly.FromDateTime(DateTime.UtcNow));
        return check;
    }

    public void DeleteEligibilityCheck(ControlNumber eligibilityCheckCtrlNbr)
    {
        var check = _eligibilityChecks.FirstOrDefault(c => c.CtrlNbr == eligibilityCheckCtrlNbr)
            ?? throw new InvalidOperationException("Eligibility check not found");

        _eligibilityChecks.Remove(check);
        RecomputeStatus(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}