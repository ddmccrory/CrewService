using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.ContactTypes;

public sealed record PhoneNumberTypeDeletedDomainEvent : DomainEvent
{
    public PhoneNumberTypeDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(PhoneNumberType), aggregateCtrlNbr.Value, payload) { }

    public PhoneNumberTypeDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(PhoneNumberType), aggregateId, payload) { }
}