using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class ShiftInstance : Entity
{
    private readonly List<PositionSlotInstance> _positionSlots = [];
    private readonly List<AssignmentNote> _assignmentNotes = [];

    public ControlNumber WorkInstanceCtrlNbr { get; private set; }
    public string ShiftCode { get; private set; } = string.Empty;
    public string ShiftDisplayName { get; private set; } = string.Empty;
    public ControlNumber? DepartmentCtrlNbr { get; private set; }
    public string? DepartmentName { get; private set; }
    public string Status { get; private set; } = "Planned";
    public bool IsComplete { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public IReadOnlyList<PositionSlotInstance> PositionSlots => _positionSlots.AsReadOnly();
    public IReadOnlyList<AssignmentNote> AssignmentNotes => _assignmentNotes.AsReadOnly();

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
        string craftRoleName,
        string groupName,
        string groupCode,
        TimeOnly onDutyTime,
        TimeOnly offDutyTime)
    {
        var status = incumbentEmployeeCtrlNbr is not null ? PositionSlotStatus.Filled : PositionSlotStatus.Open;
        var isIncumbent = incumbentEmployeeCtrlNbr is not null;
        var slot = PositionSlotInstance.Create(
            CtrlNbr, crewPositionCtrlNbr, incumbentEmployeeCtrlNbr, displayOrder, status, isIncumbent,
            assignmentCtrlNbr, assignmentCode, assignmentName, craftRoleName,
            groupName, groupCode, onDutyTime, offDutyTime);
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

    public void Reopen()
    {
        Status = "Active";
        IsComplete = false;
        CompletedAtUtc = null;
    }

    public void Cancel()
    {
        Status = "Cancelled";
    }

    public void SetAssignmentNote(ControlNumber assignmentCtrlNbr, string noteText)
    {
        var note = _assignmentNotes.SingleOrDefault(n => n.AssignmentCtrlNbr == assignmentCtrlNbr);
        if (note is null)
        {
            note = AssignmentNote.Create(CtrlNbr, assignmentCtrlNbr, noteText);
            _assignmentNotes.Add(note);
        }
        else
        {
            note.UpdateText(noteText);
        }
    }
}
