using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class CertificationEligibilityCheck : Entity
{
    public ControlNumber EmployeeCertificationCtrlNbr { get; private set; }
    public string CheckType { get; private set; } = string.Empty;
    public DateOnly EvaluationDate { get; private set; }
    public int StalenessLimitDays { get; private set; }
    public DateOnly ExpiresAtDate { get; private set; }
    public string Result { get; private set; } = string.Empty;
    public string? EvaluatorName { get; private set; }

    private CertificationEligibilityCheck()
    {
        EmployeeCertificationCtrlNbr = null!;
    }

    internal static CertificationEligibilityCheck Create(
        ControlNumber employeeCertificationCtrlNbr,
        string checkType, DateOnly evaluationDate,
        int stalenessLimitDays, string result, string? evaluatorName)
    {
        return new CertificationEligibilityCheck
        {
            EmployeeCertificationCtrlNbr = employeeCertificationCtrlNbr,
            CheckType = checkType,
            EvaluationDate = evaluationDate,
            StalenessLimitDays = stalenessLimitDays,
            ExpiresAtDate = evaluationDate.AddDays(stalenessLimitDays),
            Result = result,
            EvaluatorName = evaluatorName,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public bool IsStale(DateOnly asOfDate) => asOfDate > ExpiresAtDate;
}
