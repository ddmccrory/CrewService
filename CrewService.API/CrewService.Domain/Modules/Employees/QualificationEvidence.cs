using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Employees;

public sealed class QualificationEvidence : Entity
{
    public ControlNumber EmployeeQualificationCtrlNbr { get; private set; }
    public ControlNumber? RequirementCtrlNbr { get; private set; }
    public string EvidenceType { get; private set; } = string.Empty;
    public string EvidenceValue { get; private set; } = string.Empty;
    public DateTime RecordedAtUtc { get; private set; }
    public string RecordedBy { get; private set; } = string.Empty;

    private QualificationEvidence()
    {
        EmployeeQualificationCtrlNbr = null!;
    }

    internal static QualificationEvidence Create(
        ControlNumber employeeQualificationCtrlNbr,
        string evidenceType,
        string evidenceValue,
        string recordedBy,
        ControlNumber? RequirementCtrlNbr = null)
    {
        return new QualificationEvidence
        {
            EmployeeQualificationCtrlNbr = employeeQualificationCtrlNbr,
            RequirementCtrlNbr = RequirementCtrlNbr,
            EvidenceType = evidenceType,
            EvidenceValue = evidenceValue,
            RecordedAtUtc = DateTime.UtcNow,
            RecordedBy = recordedBy
        };
    }
}
