using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Employees;

/// <summary>
/// Records a supervisor-initiated suspension of a computed qualification for a specific employee.
/// While an active suspension exists the compute engine short-circuits to Suspended,
/// overriding any technically-met requirements.
/// </summary>
public sealed class EmployeeQualificationSuspension : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber QualificationTypeCtrlNbr { get; private set; }
    public string SuspendedBy { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTime SuspendedAtUtc { get; private set; }

    /// <summary>Optional date after which the suspension automatically lifts.</summary>
    public DateTime? AutoReinstateAtUtc { get; private set; }

    /// <summary>Set when a supervisor manually lifts the suspension early.</summary>
    public DateTime? ReinstatedAtUtc { get; private set; }
    public string? ReinstatedBy { get; private set; }
    public string? ReinstatementNote { get; private set; }

    public bool IsActive =>
        ReinstatedAtUtc is null &&
        SuspendedAtUtc <= DateTime.UtcNow &&
        (AutoReinstateAtUtc is null || AutoReinstateAtUtc.Value > DateTime.UtcNow);

    private EmployeeQualificationSuspension()
    {
        EmployeeCtrlNbr = null!;
        QualificationTypeCtrlNbr = null!;
    }

    public static EmployeeQualificationSuspension Create(
        ControlNumber employeeCtrlNbr,
        ControlNumber qualificationTypeCtrlNbr,
        string suspendedBy,
        string reason,
        DateTime? suspendedAtUtc = null,
        DateTime? autoReinstateAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new EmployeeQualificationSuspension
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            QualificationTypeCtrlNbr = qualificationTypeCtrlNbr,
            SuspendedBy = suspendedBy,
            Reason = reason,
            SuspendedAtUtc = suspendedAtUtc ?? DateTime.UtcNow,
            AutoReinstateAtUtc = autoReinstateAtUtc,
        };
    }

    public void Lift(string reinstatedBy, string? note = null)
    {
        if (!IsActive)
            throw new InvalidOperationException("Suspension is already lifted.");

        ReinstatedAtUtc = DateTime.UtcNow;
        ReinstatedBy = reinstatedBy;
        ReinstatementNote = note;
    }
}
