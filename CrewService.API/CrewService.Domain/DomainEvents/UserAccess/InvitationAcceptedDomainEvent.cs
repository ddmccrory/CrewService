using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.UserAccess;

public sealed record InvitationAcceptedDomainEvent : DomainEvent
{
    public InvitationAcceptedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(Invitation), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}
