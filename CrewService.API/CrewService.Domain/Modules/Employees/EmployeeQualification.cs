using CrewService.Domain.DomainEvents.Qualifications;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrewService.Domain.Modules.Employees;

public sealed class EmployeeQualification : Entity
{
    private readonly List<QualificationEvidence> _evidence = [];

    public const int ExpiringSoonDays = 60;

    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber QualificationTypeCtrlNbr { get; private set; }
    public DateTime? AchievedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public string GrantedBy { get; private set; } = string.Empty;
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// Computed from date fields — never stored. Revoked is the only explicit state.
    /// </summary>
    [NotMapped]
    public string Status
    {
        get
        {
            if (RevokedAtUtc is not null)                                                      return QualificationStatuses.Revoked;
            if (AchievedAtUtc is null)                                                         return QualificationStatuses.Pending;
            var now = DateTime.UtcNow;
            if (AchievedAtUtc.Value > now)                                                     return QualificationStatuses.Pending;
            if (ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= now)                           return QualificationStatuses.Expired;
            if (ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= now.AddDays(ExpiringSoonDays)) return QualificationStatuses.ExpiringSoon;
            return QualificationStatuses.Active;
        }
    }

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
        DateTime? achievedAtUtc = null)
    {
        var eq = new EmployeeQualification
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            QualificationTypeCtrlNbr = qualificationTypeCtrlNbr,
            AchievedAtUtc = achievedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            GrantedBy = grantedBy
        };

        eq.Raise(new EmployeeQualificationGrantedDomainEvent(eq));
        return eq;
    }

    public void Activate(DateTime achievedAtUtc, DateTime? expiresAtUtc = null)
    {
        AchievedAtUtc = achievedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RevokedAtUtc = null;
        RevocationReason = null;
    }

    public void Revoke(string reason)
    {
        RevokedAtUtc = DateTime.UtcNow;
        RevocationReason = reason;
        Raise(new EmployeeQualificationRevokedDomainEvent(this));
    }

    public void Reinstate(DateTime? newExpiresAtUtc = null)
    {
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
