using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record PhoneNumberUpdatedDomainEvent : DomainEvent
{
    public PhoneNumberUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(PhoneNumber), aggregateCtrlNbr.Value, payload) { }

    public PhoneNumberUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(PhoneNumber), aggregateId, payload) { }
}