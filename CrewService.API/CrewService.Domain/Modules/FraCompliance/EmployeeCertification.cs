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
    public DateTime? LastMonitoringObservationUtc { get; private set; }
    public DateTime? LastComplianceTestUtc { get; private set; }

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

    public void Activate()
    {
        Status = CertificationStatuses.Active;
    }

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

    public void Reinstate()
    {
        Status = CertificationStatuses.Active;
        SuspendedAtUtc = null;
        SuspensionReason = null;
        RevocationPeriodEndUtc = null;
    }

    public void Expire()
    {
        Status = CertificationStatuses.Expired;
    }

    public void RecordMonitoringObservation() => LastMonitoringObservationUtc = DateTime.UtcNow;

    public void RecordComplianceTest() => LastComplianceTestUtc = DateTime.UtcNow;

    public CertificationEligibilityCheck AddEligibilityCheck(
        string checkType, DateOnly evaluationDate,
        int stalenessLimitDays, string result, string? evaluatorName)
    {
        var check = CertificationEligibilityCheck.Create(
            CtrlNbr, checkType, evaluationDate, stalenessLimitDays, result, evaluatorName);
        _eligibilityChecks.Add(check);
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
        return check;
    }

    public void DeleteEligibilityCheck(ControlNumber eligibilityCheckCtrlNbr)
    {
        var check = _eligibilityChecks.FirstOrDefault(c => c.CtrlNbr == eligibilityCheckCtrlNbr)
            ?? throw new InvalidOperationException("Eligibility check not found");

        _eligibilityChecks.Remove(check);
    }
}
