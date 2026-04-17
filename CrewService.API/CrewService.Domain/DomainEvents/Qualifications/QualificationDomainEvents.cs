using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Qualifications;

public sealed record QualificationTypeCreatedDomainEvent : DomainEvent
{
    public QualificationTypeCreatedDomainEvent(QualificationType qt)
        : base(nameof(QualificationType), qt.CtrlNbr.Value,
            new { ParentCtrlNbr = qt.ParentCtrlNbr.Value, qt.Code, qt.Name, qt.EvaluationStrategy, qt.IsBlocking }) { }
}

public sealed record QualificationTypeUpdatedDomainEvent : DomainEvent
{
    public QualificationTypeUpdatedDomainEvent(QualificationType qt)
        : base(nameof(QualificationType), qt.CtrlNbr.Value,
            new { qt.Name, qt.IsBlocking, qt.IsActive }) { }
}

public sealed record EmployeeQualificationGrantedDomainEvent : DomainEvent
{
    public EmployeeQualificationGrantedDomainEvent(EmployeeQualification eq)
        : base(nameof(EmployeeQualification), eq.CtrlNbr.Value,
            new { EmployeeCtrlNbr = eq.EmployeeCtrlNbr.Value, QualificationTypeCtrlNbr = eq.QualificationTypeCtrlNbr.Value, eq.Status, eq.GrantedBy }) { }
}

public sealed record EmployeeQualificationExpiredDomainEvent : DomainEvent
{
    public EmployeeQualificationExpiredDomainEvent(EmployeeQualification eq)
        : base(nameof(EmployeeQualification), eq.CtrlNbr.Value,
            new { EmployeeCtrlNbr = eq.EmployeeCtrlNbr.Value, QualificationTypeCtrlNbr = eq.QualificationTypeCtrlNbr.Value }) { }
}

public sealed record EmployeeQualificationRevokedDomainEvent : DomainEvent
{
    public EmployeeQualificationRevokedDomainEvent(EmployeeQualification eq)
        : base(nameof(EmployeeQualification), eq.CtrlNbr.Value,
            new { EmployeeCtrlNbr = eq.EmployeeCtrlNbr.Value, QualificationTypeCtrlNbr = eq.QualificationTypeCtrlNbr.Value, eq.RevocationReason }) { }
}

public sealed record EmployeeQualificationExpiringSoonDomainEvent : DomainEvent
{
    public EmployeeQualificationExpiringSoonDomainEvent(EmployeeQualification eq, int daysRemaining)
        : base(nameof(EmployeeQualification), eq.CtrlNbr.Value,
            new { EmployeeCtrlNbr = eq.EmployeeCtrlNbr.Value, QualificationTypeCtrlNbr = eq.QualificationTypeCtrlNbr.Value, DaysRemaining = daysRemaining }) { }
}
