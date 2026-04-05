using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class PositionSlotInstance : Entity
{
    public ControlNumber ShiftInstanceCtrlNbr { get; private set; }
    public ControlNumber CrewPositionCtrlNbr { get; private set; }
    public ControlNumber? IncumbentEmployeeCtrlNbr { get; private set; }
    public ControlNumber AssignmentCtrlNbr { get; private set; }
    public string AssignmentCode { get; private set; } = string.Empty;
    public string AssignmentName { get; private set; } = string.Empty;
    public string CraftRoleName { get; private set; } = string.Empty;
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
        AssignmentCtrlNbr = null!;
    }

    internal static PositionSlotInstance Create(
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber crewPositionCtrlNbr,
        ControlNumber? incumbentEmployeeCtrlNbr,
        int displayOrder,
        string status,
        ControlNumber assignmentCtrlNbr,
        string assignmentCode,
        string assignmentName,
        string craftRoleName)
    {
        return new PositionSlotInstance
        {
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            CrewPositionCtrlNbr = crewPositionCtrlNbr,
            IncumbentEmployeeCtrlNbr = incumbentEmployeeCtrlNbr,
            DisplayOrder = displayOrder,
            Status = status,
            AssignmentCtrlNbr = assignmentCtrlNbr,
            AssignmentCode = assignmentCode,
            AssignmentName = assignmentName,
            CraftRoleName = craftRoleName
        };
    }

    public void Fill(ControlNumber employeeCtrlNbr)
    {
        IncumbentEmployeeCtrlNbr = employeeCtrlNbr;
        Status = "Filled";
    }

    public void MarkOnDuty()
    {
        Status = "OnDuty";
    }

    public void MarkTiedUp()
    {
        Status = "TiedUp";
    }

    public void Annul(string reason)
    {
        IsAnnulled = true;
        AnnulmentReason = reason;
        Status = "Annulled";
    }

    public void MarkDoNotFill()
    {
        IsDoNotFill = true;
        Status = "DoNotFill";
    }

    public void Skip()
    {
        IsSkipped = true;
        Status = "Skipped";
    }
}
