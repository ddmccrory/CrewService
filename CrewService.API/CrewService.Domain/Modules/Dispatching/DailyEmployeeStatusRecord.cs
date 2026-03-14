using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Dispatching;

public sealed class DailyEmployeeStatusRecord : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public DateOnly RecordDate { get; private set; }
    public string StatusCode { get; private set; } = string.Empty;
    public string? SnapshotJson { get; private set; }

    private DailyEmployeeStatusRecord()
    {
        EmployeeCtrlNbr = null!;
        WorkAreaGroupCtrlNbr = null!;
    }

    public static DailyEmployeeStatusRecord Create(
        ControlNumber employeeCtrlNbr, ControlNumber workAreaGroupCtrlNbr,
        DateOnly recordDate, string statusCode, string? snapshotJson = null)
    {
        return new DailyEmployeeStatusRecord
        {
            EmployeeCtrlNbr = employeeCtrlNbr,
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            RecordDate = recordDate,
            StatusCode = statusCode,
            SnapshotJson = snapshotJson
        };
    }
}
