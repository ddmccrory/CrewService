using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record EmailAddressUpdatedDomainEvent : DomainEvent
{
    public EmailAddressUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(EmailAddress), aggregateCtrlNbr.Value, payload) { }

    public EmailAddressUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(EmailAddress), aggregateId, payload) { }
}