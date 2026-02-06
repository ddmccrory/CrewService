using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.ContactTypes;

public sealed record AddressTypeCreatedDomainEvent : DomainEvent
{
    public AddressTypeCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(AddressType), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}