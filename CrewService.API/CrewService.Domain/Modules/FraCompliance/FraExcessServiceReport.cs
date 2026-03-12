using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class FraExcessServiceReport : Entity
{
    public ControlNumber DutyTourCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string ViolationType { get; private set; } = string.Empty;
    public DateTime DetectedAtUtc { get; private set; }
    public string? ExplanationText { get; private set; }
    public bool ReportedToFra { get; private set; }
    public DateTime? ReportedAtUtc { get; private set; }

    private FraExcessServiceReport()
    {
        DutyTourCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static FraExcessServiceReport Create(
        ControlNumber dutyTourCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string violationType,
        string? explanationText = null)
    {
        return new FraExcessServiceReport
        {
            DutyTourCtrlNbr = dutyTourCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            ViolationType = violationType,
            DetectedAtUtc = DateTime.UtcNow,
            ExplanationText = explanationText,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public void MarkReported()
    {
        ReportedToFra = true;
        ReportedAtUtc = DateTime.UtcNow;
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }
}
