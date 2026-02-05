using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record PhoneNumberCreatedDomainEvent : DomainEvent
{
    public PhoneNumberCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(PhoneNumber), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}