using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record PhoneNumberDeletedDomainEvent : DomainEvent
{
    public PhoneNumberDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(PhoneNumber), aggregateCtrlNbr.Value, payload) { }

    public PhoneNumberDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(PhoneNumber), aggregateId, payload) { }
}