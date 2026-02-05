using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.ContactTypes;

public sealed record PhoneNumberTypeCreatedDomainEvent : DomainEvent
{
    public PhoneNumberTypeCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(PhoneNumberType), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}