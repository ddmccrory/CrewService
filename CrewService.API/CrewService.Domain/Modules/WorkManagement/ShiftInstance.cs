using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class ShiftInstance : Entity
{
    private readonly List<PositionSlotInstance> _positionSlots = [];

    public ControlNumber WorkInstanceCtrlNbr { get; private set; }
    public string ShiftCode { get; private set; } = string.Empty;
    public string ShiftDisplayName { get; private set; } = string.Empty;
    public ControlNumber? DepartmentCtrlNbr { get; private set; }
    public string? DepartmentName { get; private set; }
    public string Status { get; private set; } = "Planned";
    public bool IsComplete { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public IReadOnlyList<PositionSlotInstance> PositionSlots => _positionSlots.AsReadOnly();

    private ShiftInstance()
    {
        WorkInstanceCtrlNbr = null!;
    }

    public static ShiftInstance Create(
        ControlNumber workInstanceCtrlNbr,
        string shiftCode,
        string shiftDisplayName,
        ControlNumber? departmentCtrlNbr = null,
        string? departmentName = null)
    {
        return new ShiftInstance
        {
            WorkInstanceCtrlNbr = workInstanceCtrlNbr,
            ShiftCode = shiftCode,
            ShiftDisplayName = shiftDisplayName,
            DepartmentCtrlNbr = departmentCtrlNbr,
            DepartmentName = departmentName,
            Status = "Planned"
        };
    }

    public PositionSlotInstance AddPositionSlot(
        ControlNumber crewPositionCtrlNbr,
        ControlNumber? incumbentEmployeeCtrlNbr,
        int displayOrder,
        ControlNumber assignmentCtrlNbr,
        string assignmentCode,
        string assignmentName,
        string craftRoleName)
    {
        var status = incumbentEmployeeCtrlNbr is not null ? "Filled" : "Open";
        var slot = PositionSlotInstance.Create(
            CtrlNbr, crewPositionCtrlNbr, incumbentEmployeeCtrlNbr, displayOrder, status,
            assignmentCtrlNbr, assignmentCode, assignmentName, craftRoleName);
        _positionSlots.Add(slot);
        return slot;
    }

    public void Activate()
    {
        Status = "Active";
    }

    public void Complete()
    {
        Status = "Completed";
        IsComplete = true;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = "Cancelled";
    }
}
