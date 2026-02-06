using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.ContactTypes;

public sealed record AddressTypeUpdatedDomainEvent : DomainEvent
{
    public AddressTypeUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(AddressType), aggregateCtrlNbr.Value, payload) { }

    public AddressTypeUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(AddressType), aggregateId, payload) { }
}