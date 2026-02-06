using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record AddressUpdatedDomainEvent : DomainEvent
{
    public AddressUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Address), aggregateCtrlNbr.Value, payload) { }

    public AddressUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Address), aggregateId, payload) { }
}