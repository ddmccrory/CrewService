using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.ContactTypes;

public sealed record EmailAddressTypeCreatedDomainEvent : DomainEvent
{
    public EmailAddressTypeCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(EmailAddressType), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}