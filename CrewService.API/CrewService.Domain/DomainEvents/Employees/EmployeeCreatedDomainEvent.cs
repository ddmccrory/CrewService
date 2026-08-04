using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record EmployeeCreatedDomainEvent : DomainEvent
{
    public EmployeeCreatedDomainEvent(
        ControlNumber aggregateCtrlNbr,
        ControlNumber clientCtrlNbr,
        ControlNumber? railroadCtrlNbr,
        string email,
        string invitedByUserId,
        string invitedByUserName,
        string parentName)
        : base(nameof(Employee), aggregateCtrlNbr.Value,
            payload: new
            {
                AggregateCtrlNbr = aggregateCtrlNbr.Value,
                ClientCtrlNbr = clientCtrlNbr.Value,
                RailroadCtrlNbr = railroadCtrlNbr?.Value,
                Email = email,
                InvitedByUserId = invitedByUserId,
                InvitedByUserName = invitedByUserName,
                ParentName = parentName
            }) { }
}
