using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.UserAccess;

public sealed record InvitationRevokedDomainEvent : DomainEvent
{
    public InvitationRevokedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(Invitation), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}
