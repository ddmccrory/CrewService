using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.UserAccess;

public sealed record UserParentAssignmentCreatedDomainEvent : DomainEvent
{
    public UserParentAssignmentCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(UserParentAssignment), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}
