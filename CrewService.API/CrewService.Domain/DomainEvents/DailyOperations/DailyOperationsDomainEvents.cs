using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.DailyOperations;

public sealed record ShiftInstanceCreatedDomainEvent : DomainEvent
{
    public ShiftInstanceCreatedDomainEvent(ControlNumber shiftCtrlNbr, ControlNumber workInstanceCtrlNbr, string shiftCode)
        : base("ShiftInstance", shiftCtrlNbr.Value,
            payload: new { ShiftCtrlNbr = shiftCtrlNbr.Value, WorkInstanceCtrlNbr = workInstanceCtrlNbr.Value, ShiftCode = shiftCode }) { }
}

public sealed record PositionSlotStatusChangedDomainEvent : DomainEvent
{
    public PositionSlotStatusChangedDomainEvent(ControlNumber slotCtrlNbr, string oldStatus, string newStatus)
        : base("PositionSlotInstance", slotCtrlNbr.Value,
            payload: new { SlotCtrlNbr = slotCtrlNbr.Value, OldStatus = oldStatus, NewStatus = newStatus }) { }
}

public sealed record PositionSlotAnnulledDomainEvent : DomainEvent
{
    public PositionSlotAnnulledDomainEvent(ControlNumber slotCtrlNbr, string reason)
        : base("PositionSlotInstance", slotCtrlNbr.Value,
            payload: new { SlotCtrlNbr = slotCtrlNbr.Value, Reason = reason }) { }
}

public sealed record OnDutyRecordCreatedDomainEvent : DomainEvent
{
    public OnDutyRecordCreatedDomainEvent(ControlNumber recordCtrlNbr, ControlNumber employeeCtrlNbr, ControlNumber positionSlotCtrlNbr)
        : base("OnDutyRecord", recordCtrlNbr.Value,
            payload: new { RecordCtrlNbr = recordCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value, PositionSlotCtrlNbr = positionSlotCtrlNbr.Value }) { }
}

public sealed record OffDutyRecordCreatedDomainEvent : DomainEvent
{
    public OffDutyRecordCreatedDomainEvent(ControlNumber recordCtrlNbr, ControlNumber employeeCtrlNbr, ControlNumber onDutyRecordCtrlNbr)
        : base("OffDutyRecord", recordCtrlNbr.Value,
            payload: new { RecordCtrlNbr = recordCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value, OnDutyRecordCtrlNbr = onDutyRecordCtrlNbr.Value }) { }
}

public sealed record CallSheetGeneratedDomainEvent : DomainEvent
{
    public CallSheetGeneratedDomainEvent(ControlNumber workInstanceCtrlNbr, int shiftCount, int totalSlots)
        : base("WorkInstance", workInstanceCtrlNbr.Value,
            payload: new { WorkInstanceCtrlNbr = workInstanceCtrlNbr.Value, ShiftCount = shiftCount, TotalSlots = totalSlots }) { }
}
