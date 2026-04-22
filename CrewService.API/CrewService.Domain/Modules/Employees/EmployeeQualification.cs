using CrewService.Domain.DomainEvents.Qualifications;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Employees;

public sealed class EmployeeQualification : Entity
{
    private readonly List<QualificationEvidence> _evidence = [];

    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber QualificationTypeCtrlNbr { get; private set; }
    public DateTime AchievedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public string Status { get; private set; } = "Pending";
    public string GrantedBy { get; private set; } = string.Empty;
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }

    public IReadOnlyList<QualificationEvidence> Evidence => _evidence.AsReadOnly();

    private EmployeeQualification()
    {
        EmployeeCtrlNbr = null!;
        QualificationTypeCtrlNbr = null!;
    }

    public static EmployeeQualification Create(
        ControlNumber employeeCtrlNbr,
        ControlNumber qualificationTypeCtrlNbr,
        string grantedBy,
        DateTime? expiresAtUtc = null,
        string status = "Active")
    {
        var eq = new EmployeeQualification
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            QualificationTypeCtrlNbr = qualificationTypeCtrlNbr,
            AchievedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
            Status = status,
            GrantedBy = grantedBy
        };

        eq.Raise(new EmployeeQualificationGrantedDomainEvent(eq));
        return eq;
    }

    public void Activate()
    {
        Status = "Active";
    }

    public void MarkExpiringSoon(int daysRemaining)
    {
        if (Status is "Active")
        {
            Status = "ExpiringSoon";
            Raise(new EmployeeQualificationExpiringSoonDomainEvent(this, daysRemaining));
        }
    }

    public void Expire()
    {
        Status = "Expired";
        Raise(new EmployeeQualificationExpiredDomainEvent(this));
    }

    public void Revoke(string reason)
    {
        Status = "Revoked";
        RevokedAtUtc = DateTime.UtcNow;
        RevocationReason = reason;
        Raise(new EmployeeQualificationRevokedDomainEvent(this));
    }

    public void Reinstate(DateTime? newExpiresAtUtc = null)
    {
        Status = "Active";
        RevokedAtUtc = null;
        RevocationReason = null;
        if (newExpiresAtUtc.HasValue)
            ExpiresAtUtc = newExpiresAtUtc.Value;
    }

    public QualificationEvidence AddEvidence(
        string evidenceType,
        string evidenceValue,
        string recordedBy,
        ControlNumber? requirementCtrlNbr = null)
    {
        var evidence = QualificationEvidence.Create(
            CtrlNbr,
            evidenceType,
            evidenceValue,
            recordedBy,
            requirementCtrlNbr);

        _evidence.Add(evidence);
        return evidence;
    }
}
