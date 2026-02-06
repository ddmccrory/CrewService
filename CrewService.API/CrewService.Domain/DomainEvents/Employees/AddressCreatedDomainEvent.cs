using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record AddressCreatedDomainEvent : DomainEvent
{
    public AddressCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(Address), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}