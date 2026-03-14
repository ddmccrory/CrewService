using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class FraMonthlyAccumulator : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string YearMonth { get; private set; } = string.Empty;
    public int CoveredServiceMinutes { get; private set; }
    public int DeadheadToReleaseMinutes { get; private set; }
    public int OtherServiceMinutes { get; private set; }
    public int DeadheadAfter12hMinutes { get; private set; }

    private FraMonthlyAccumulator()
    {
        EmployeeCtrlNbr = null!;
    }

    public static FraMonthlyAccumulator Create(
        ControlNumber employeeCtrlNbr,
        string yearMonth)
    {
        return new FraMonthlyAccumulator
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            YearMonth = yearMonth
        };
    }

    public void AddTourMinutes(
        int coveredServiceMinutes,
        int deadheadToReleaseMinutes,
        int otherServiceMinutes,
        int deadheadAfter12hMinutes)
    {
        CoveredServiceMinutes += coveredServiceMinutes;
        DeadheadToReleaseMinutes += deadheadToReleaseMinutes;
        OtherServiceMinutes += otherServiceMinutes;
        DeadheadAfter12hMinutes += deadheadAfter12hMinutes;
    }

    public int TotalMinutes => CoveredServiceMinutes + DeadheadToReleaseMinutes + OtherServiceMinutes;
}
