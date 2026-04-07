using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Staffing;

public sealed class StaffablePosition : Entity
{
    public string PositionType { get; private set; } = string.Empty;

    private StaffablePosition() { }

    public static StaffablePosition Create(string positionType)
    {
        var position = new StaffablePosition
        {
            PositionType = positionType
        };
        position.Raise(new StaffablePositionCreatedDomainEvent(
            position.CtrlNbr, positionType));
        return position;
    }
}

public sealed class PositionAssignment : Entity
{
    public ControlNumber StaffablePositionCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string AssignmentType { get; private set; } = string.Empty;
    public ControlNumber? AssignmentSourceCtrlNbr { get; private set; }
    public DateTime AssignedDateUtc { get; private set; }

    private PositionAssignment()
    {
        StaffablePositionCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static PositionAssignment Create(
        ControlNumber staffablePositionCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string assignmentType,
        ControlNumber? assignmentSourceCtrlNbr = null)
    {
        var assignment = new PositionAssignment
        {
            StaffablePositionCtrlNbr = staffablePositionCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            AssignmentType = assignmentType,
            AssignmentSourceCtrlNbr = assignmentSourceCtrlNbr,
            AssignedDateUtc = DateTime.UtcNow
        };
        assignment.Raise(new PositionAssignmentCreatedDomainEvent(
            staffablePositionCtrlNbr, employeeCtrlNbr, assignmentType));
        return assignment;
    }

    public void Vacate()
    {
        Raise(new PositionAssignmentVacatedDomainEvent(
            StaffablePositionCtrlNbr, EmployeeCtrlNbr));
    }
}

// Domain Events
public sealed record StaffablePositionCreatedDomainEvent : DomainEvent
{
    public StaffablePositionCreatedDomainEvent(ControlNumber positionCtrlNbr, string positionType)
        : base("StaffablePosition", positionCtrlNbr.Value,
            payload: new { PositionCtrlNbr = positionCtrlNbr.Value, PositionType = positionType }) { }
}

public sealed record PositionAssignmentCreatedDomainEvent : DomainEvent
{
    public PositionAssignmentCreatedDomainEvent(
        ControlNumber staffablePositionCtrlNbr, ControlNumber employeeCtrlNbr, string assignmentType)
        : base("PositionAssignment", staffablePositionCtrlNbr.Value,
            payload: new
            {
                StaffablePositionCtrlNbr = staffablePositionCtrlNbr.Value,
                EmployeeCtrlNbr = employeeCtrlNbr.Value,
                AssignmentType = assignmentType
            }) { }
}

public sealed record PositionAssignmentVacatedDomainEvent : DomainEvent
{
    public PositionAssignmentVacatedDomainEvent(
        ControlNumber staffablePositionCtrlNbr, ControlNumber employeeCtrlNbr)
        : base("PositionAssignment", staffablePositionCtrlNbr.Value,
            payload: new
            {
                StaffablePositionCtrlNbr = staffablePositionCtrlNbr.Value,
                EmployeeCtrlNbr = employeeCtrlNbr.Value
            }) { }
}
