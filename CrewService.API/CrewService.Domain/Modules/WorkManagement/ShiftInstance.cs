using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class ShiftInstance : Entity
{
    private readonly List<PositionSlotInstance> _positionSlots = [];
    private readonly List<BoardSlotInstance> _boardSlots = [];
    private readonly List<AssignmentNote> _assignmentNotes = [];

    public ControlNumber WorkInstanceCtrlNbr { get; private set; }
    public ControlNumber ShiftDefinitionCtrlNbr { get; private set; }
    public string ShiftCode { get; private set; } = string.Empty;
    public string ShiftDisplayName { get; private set; } = string.Empty;
    public ControlNumber? DepartmentCtrlNbr { get; private set; }
    public string? DepartmentName { get; private set; }
    public string Status { get; private set; } = "Planned";
    public bool IsComplete { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public IReadOnlyList<PositionSlotInstance> PositionSlots => _positionSlots.AsReadOnly();
    public IReadOnlyList<BoardSlotInstance> BoardSlots => _boardSlots.AsReadOnly();
    public IReadOnlyList<AssignmentNote> AssignmentNotes => _assignmentNotes.AsReadOnly();

    private ShiftInstance()
    {
        WorkInstanceCtrlNbr = null!;
        ShiftDefinitionCtrlNbr = null!;
    }

    public static ShiftInstance Create(
        ControlNumber workInstanceCtrlNbr,
        ControlNumber shiftDefinitionCtrlNbr,
        string shiftCode,
        string shiftDisplayName,
        ControlNumber? departmentCtrlNbr = null,
        string? departmentName = null)
    {
        var instance = new ShiftInstance
        {
            WorkInstanceCtrlNbr = workInstanceCtrlNbr,
            ShiftDefinitionCtrlNbr = shiftDefinitionCtrlNbr,
            ShiftCode = shiftCode,
            ShiftDisplayName = shiftDisplayName,
            DepartmentCtrlNbr = departmentCtrlNbr,
            DepartmentName = departmentName,
            Status = "Planned"
        };
        instance.Raise(new ShiftInstanceCreatedDomainEvent(instance));
        return instance;
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
        TimeOnly offDutyTime,
        string crewName = "",
        string crewType = "")
    {
        var status = incumbentEmployeeCtrlNbr is not null ? PositionSlotStatus.Filled : PositionSlotStatus.Open;
        var isIncumbent = incumbentEmployeeCtrlNbr is not null;
        var slot = PositionSlotInstance.Create(
            CtrlNbr, crewPositionCtrlNbr, incumbentEmployeeCtrlNbr, displayOrder, status, isIncumbent,
            assignmentCtrlNbr, assignmentCode, assignmentName, craftRoleName,
            groupName, groupCode, onDutyTime, offDutyTime,
            crewName, crewType);
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

    public PositionSlotInstance AddAdHocPositionSlot(ControlNumber assignmentCtrlNbr, string craftRoleName)
    {
        var existing = _positionSlots.FirstOrDefault(s => s.AssignmentCtrlNbr == assignmentCtrlNbr)
            ?? throw new InvalidOperationException($"No existing positions found for assignment {assignmentCtrlNbr} to copy metadata from.");

        var maxOrder = _positionSlots
            .Where(s => s.AssignmentCtrlNbr == assignmentCtrlNbr && s.CraftRoleName == craftRoleName)
            .Select(s => s.DisplayOrder)
            .DefaultIfEmpty(0)
            .Max();

        var slot = PositionSlotInstance.CreateAdHoc(
            CtrlNbr,
            maxOrder + 1,
            existing.AssignmentCtrlNbr,
            existing.AssignmentCode,
            existing.AssignmentName,
            craftRoleName,
            existing.GroupName,
            existing.GroupCode,
            existing.OnDutyTime,
            existing.OffDutyTime,
            existing.CrewName,
            existing.CrewType);
        _positionSlots.Add(slot);
        return slot;
    }

    public void RemovePositionSlot(ControlNumber positionSlotCtrlNbr)
    {
        var slot = _positionSlots.SingleOrDefault(s => s.CtrlNbr == positionSlotCtrlNbr)
            ?? throw new InvalidOperationException($"Position slot {positionSlotCtrlNbr} not found.");

        if (!slot.IsAdHoc)
            throw new InvalidOperationException("Only ad-hoc positions can be removed.");

        if (slot.Status != PositionSlotStatus.Open)
            throw new InvalidOperationException("Only open positions can be removed.");

        _positionSlots.Remove(slot);
    }

    public void ReorderPositionSlots(IEnumerable<(ControlNumber CtrlNbr, int DisplayOrder)> orders)
    {
        foreach (var (ctrlNbr, displayOrder) in orders)
        {
            var slot = _positionSlots.SingleOrDefault(s => s.CtrlNbr == ctrlNbr);
            slot?.SetDisplayOrder(displayOrder);
        }
    }

    public void AddTemplateAssignment(
        ControlNumber assignmentCtrlNbr,
        string assignmentCode,
        string assignmentName,
        string groupName,
        string groupCode,
        TimeOnly onDutyTime,
        TimeOnly offDutyTime,
        IReadOnlyList<(ControlNumber PositionCtrlNbr, ControlNumber? IncumbentEmployeeCtrlNbr, int DisplayOrder, string CraftRoleName, string CrewName, string CrewType)> positions)
    {
        if (_positionSlots.Any(s => s.AssignmentCtrlNbr == assignmentCtrlNbr))
            throw new InvalidOperationException($"Assignment {assignmentCtrlNbr} is already on this shift.");

        foreach (var pos in positions)
        {
            AddPositionSlot(
                pos.PositionCtrlNbr, pos.IncumbentEmployeeCtrlNbr, pos.DisplayOrder,
                assignmentCtrlNbr, assignmentCode, assignmentName,
                pos.CraftRoleName, groupName, groupCode, onDutyTime, offDutyTime,
                pos.CrewName, pos.CrewType);
        }
    }

    public void AddAdHocAssignment(
        string assignmentCode,
        string assignmentName,
        string groupName,
        string groupCode,
        TimeOnly onDutyTime,
        TimeOnly offDutyTime,
        IReadOnlyList<string> craftRoleNames)
    {
        if (craftRoleNames.Count == 0)
            throw new InvalidOperationException("At least one craft/role is required for an ad-hoc assignment.");

        var syntheticCtrlNbr = ControlNumber.Create();

        for (var i = 0; i < craftRoleNames.Count; i++)
        {
            var slot = PositionSlotInstance.CreateAdHoc(
                CtrlNbr, i + 1,
                syntheticCtrlNbr, assignmentCode, assignmentName,
                craftRoleNames[i], groupName, groupCode, onDutyTime, offDutyTime);
            _positionSlots.Add(slot);
        }
    }

    public void RemoveAssignment(ControlNumber assignmentCtrlNbr)
    {
        var slots = _positionSlots.Where(s => s.AssignmentCtrlNbr == assignmentCtrlNbr).ToList();

        if (slots.Count == 0)
            throw new InvalidOperationException($"No positions found for assignment {assignmentCtrlNbr}.");

        var activeStatuses = new[]
        {
            PositionSlotStatus.OnDuty,
            PositionSlotStatus.OnDutyOvertime,
            PositionSlotStatus.TiedUp
        };

        if (slots.Any(s => activeStatuses.Contains(s.Status)))
            throw new InvalidOperationException("Cannot remove an assignment that has positions on duty or tied up.");

        foreach (var slot in slots)
            _positionSlots.Remove(slot);

        var note = _assignmentNotes.SingleOrDefault(n => n.AssignmentCtrlNbr == assignmentCtrlNbr);
        if (note is not null)
            _assignmentNotes.Remove(note);
    }

    public BoardSlotInstance AddBoardSlot(
        ControlNumber rosterBoardCtrlNbr,
        ControlNumber? rosterBoardPositionCtrlNbr,
        ControlNumber employeeCtrlNbr,
        int boardOrder,
        long callSequence,
        string boardName,
        string employeeName,
        string positionName = "",
        int daysWorked = 0,
        int consecutiveDays = 0,
        DateTime? restAvailableAtUtc = null)
    {
        var slot = BoardSlotInstance.Create(
            CtrlNbr, rosterBoardCtrlNbr, rosterBoardPositionCtrlNbr,
            employeeCtrlNbr, boardOrder, callSequence,
            boardName, employeeName, positionName,
            daysWorked, consecutiveDays, restAvailableAtUtc);
        _boardSlots.Add(slot);
        return slot;
    }
}

public sealed record ShiftInstanceCreatedDomainEvent : DomainEvent
{
    public ShiftInstanceCreatedDomainEvent(ShiftInstance s)
        : base(nameof(ShiftInstance), s.CtrlNbr.Value, new { s.ShiftCode, s.ShiftDisplayName, WorkInstanceCtrlNbr = s.WorkInstanceCtrlNbr.Value }) { }
}
