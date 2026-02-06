using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record AddressDeletedDomainEvent : DomainEvent
{
    public AddressDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Address), aggregateCtrlNbr.Value, payload) { }

    public AddressDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Address), aggregateId, payload) { }
}