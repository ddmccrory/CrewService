using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.ContactTypes;

public sealed record PhoneNumberTypeUpdatedDomainEvent : DomainEvent
{
    public PhoneNumberTypeUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(PhoneNumberType), aggregateCtrlNbr.Value, payload) { }

    public PhoneNumberTypeUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(PhoneNumberType), aggregateId, payload) { }
}