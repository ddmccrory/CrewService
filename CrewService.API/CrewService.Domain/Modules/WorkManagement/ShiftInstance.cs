using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class ShiftInstance : Entity
{
    private readonly List<PositionSlotInstance> _positionSlots = [];

    public ControlNumber WorkInstanceCtrlNbr { get; private set; }
    public string ShiftCode { get; private set; } = string.Empty;
    public DateTime ShiftStartUtc { get; private set; }
    public DateTime ShiftEndUtc { get; private set; }
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
        DateTime shiftStartUtc,
        DateTime shiftEndUtc)
    {
        return new ShiftInstance
        {
            WorkInstanceCtrlNbr = workInstanceCtrlNbr,
            ShiftCode = shiftCode,
            ShiftStartUtc = shiftStartUtc,
            ShiftEndUtc = shiftEndUtc,
            Status = "Planned"
        };
    }

    public PositionSlotInstance AddPositionSlot(
        ControlNumber crewPositionCtrlNbr,
        ControlNumber? incumbentEmployeeCtrlNbr,
        int displayOrder)
    {
        var status = incumbentEmployeeCtrlNbr is not null ? "Filled" : "Open";
        var slot = PositionSlotInstance.Create(
            CtrlNbr, crewPositionCtrlNbr, incumbentEmployeeCtrlNbr, displayOrder, status);
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
