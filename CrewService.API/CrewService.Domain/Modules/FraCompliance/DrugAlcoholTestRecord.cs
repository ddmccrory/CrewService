using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class DrugAlcoholTestRecord : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string TestType { get; private set; } = string.Empty;
    public DateTime TestDate { get; private set; }
    public decimal? AlcoholResult { get; private set; }
    public string? DrugResult { get; private set; }
    public string? SubstancesDetected { get; private set; }
    public bool IsViolation { get; private set; }
    public bool FederalAuthority { get; private set; }

    private DrugAlcoholTestRecord()
    {
        EmployeeCtrlNbr = null!;
    }

    public static DrugAlcoholTestRecord Create(
        ControlNumber employeeCtrlNbr,
        string testType,
        DateTime testDate,
        decimal? alcoholResult,
        string? drugResult,
        string? substancesDetected,
        bool federalAuthority)
    {
        var isViolation = (alcoholResult.HasValue && alcoholResult.Value >= 0.04m)
                       || drugResult == "Positive"
                       || drugResult == "Refused";

        return new DrugAlcoholTestRecord
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            TestType = testType,
            TestDate = testDate,
            AlcoholResult = alcoholResult,
            DrugResult = drugResult,
            SubstancesDetected = substancesDetected,
            IsViolation = isViolation,
            FederalAuthority = federalAuthority
        };
    }

    /// <summary>
    /// Returns true if alcohol is in 0.02-0.039 range (removal but not violation).
    /// </summary>
    public bool IsAlcoholRemovalRange =>
        AlcoholResult.HasValue && AlcoholResult.Value >= 0.02m && AlcoholResult.Value < 0.04m;
}
