using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class PositionSlotInstance : Entity
{
    public ControlNumber ShiftInstanceCtrlNbr { get; private set; }
    public ControlNumber CrewPositionCtrlNbr { get; private set; }
    public ControlNumber? IncumbentEmployeeCtrlNbr { get; private set; }
    public string Status { get; private set; } = "Open";
    public bool IsAnnulled { get; private set; }
    public bool IsDoNotFill { get; private set; }
    public bool IsSkipped { get; private set; }
    public string? AnnulmentReason { get; private set; }
    public int DisplayOrder { get; private set; }

    private PositionSlotInstance()
    {
        ShiftInstanceCtrlNbr = null!;
        CrewPositionCtrlNbr = null!;
    }

    internal static PositionSlotInstance Create(
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber crewPositionCtrlNbr,
        ControlNumber? incumbentEmployeeCtrlNbr,
        int displayOrder,
        string status)
    {
        return new PositionSlotInstance
        {
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            CrewPositionCtrlNbr = crewPositionCtrlNbr,
            IncumbentEmployeeCtrlNbr = incumbentEmployeeCtrlNbr,
            DisplayOrder = displayOrder,
            Status = status,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public void Fill(ControlNumber employeeCtrlNbr)
    {
        IncumbentEmployeeCtrlNbr = employeeCtrlNbr;
        Status = "Filled";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void MarkOnDuty()
    {
        Status = "OnDuty";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void MarkTiedUp()
    {
        Status = "TiedUp";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void Annul(string reason)
    {
        IsAnnulled = true;
        AnnulmentReason = reason;
        Status = "Annulled";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void MarkDoNotFill()
    {
        IsDoNotFill = true;
        Status = "DoNotFill";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void Skip()
    {
        IsSkipped = true;
        Status = "Skipped";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }
}
