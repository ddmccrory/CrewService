using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.ContactTypes;

public sealed record AddressTypeDeletedDomainEvent : DomainEvent
{
    public AddressTypeDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(AddressType), aggregateCtrlNbr.Value, payload) { }

    public AddressTypeDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(AddressType), aggregateId, payload) { }
}