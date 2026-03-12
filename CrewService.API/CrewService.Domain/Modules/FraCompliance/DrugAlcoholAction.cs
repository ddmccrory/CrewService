using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.FraCompliance;

public sealed class DrugAlcoholAction : Entity
{
    public ControlNumber TestRecordCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string ActionType { get; private set; } = string.Empty;
    public DateTime ActionDate { get; private set; }
    public string? Notes { get; private set; }

    private DrugAlcoholAction()
    {
        TestRecordCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static DrugAlcoholAction Create(
        ControlNumber testRecordCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string actionType,
        string? notes = null)
    {
        return new DrugAlcoholAction
        {
            TestRecordCtrlNbr = testRecordCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            ActionType = actionType,
            ActionDate = DateTime.UtcNow,
            Notes = notes,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }
}
