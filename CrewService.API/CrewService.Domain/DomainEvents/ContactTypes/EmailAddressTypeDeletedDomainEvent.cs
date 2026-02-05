using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.ContactTypes;

public sealed record EmailAddressTypeDeletedDomainEvent : DomainEvent
{
    public EmailAddressTypeDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(EmailAddressType), aggregateCtrlNbr.Value, payload) { }

    public EmailAddressTypeDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(EmailAddressType), aggregateId, payload) { }
}