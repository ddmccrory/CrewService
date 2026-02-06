using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.ContactTypes;

public sealed record EmailAddressTypeUpdatedDomainEvent : DomainEvent
{
    public EmailAddressTypeUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(EmailAddressType), aggregateCtrlNbr.Value, payload) { }

    public EmailAddressTypeUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(EmailAddressType), aggregateId, payload) { }
}