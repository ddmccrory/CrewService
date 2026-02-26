using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.UserAccess;

public sealed record InvitationCreatedDomainEvent : DomainEvent
{
    public InvitationCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(Invitation), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}
