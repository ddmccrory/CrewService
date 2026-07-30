using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Boards;

public sealed record RosterBoardCreatedDomainEvent : DomainEvent
{
    public RosterBoardCreatedDomainEvent(ControlNumber boardCtrlNbr, string name)
        : base("RosterBoard", boardCtrlNbr.Value,
            payload: new { BoardCtrlNbr = boardCtrlNbr.Value, Name = name }) { }
}

public sealed record PositionMarkedOffDomainEvent : DomainEvent
{
    public PositionMarkedOffDomainEvent(ControlNumber positionCtrlNbr, ControlNumber employeeCtrlNbr)
        : base("RosterBoardPosition", positionCtrlNbr.Value,
            payload: new { PositionCtrlNbr = positionCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value }) { }
}

public sealed record PositionRestoredDomainEvent : DomainEvent
{
    public PositionRestoredDomainEvent(ControlNumber positionCtrlNbr, ControlNumber employeeCtrlNbr)
        : base("RosterBoardPosition", positionCtrlNbr.Value,
            payload: new { PositionCtrlNbr = positionCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value }) { }
}

public sealed record PositionsReorderedDomainEvent : DomainEvent
{
    public PositionsReorderedDomainEvent(ControlNumber boardCtrlNbr, object changes)
        : base("RosterBoard", boardCtrlNbr.Value,
            payload: new { BoardCtrlNbr = boardCtrlNbr.Value, Changes = changes }) { }
}
