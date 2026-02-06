using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record EmailAddressDeletedDomainEvent : DomainEvent
{
    public EmailAddressDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(EmailAddress), aggregateCtrlNbr.Value, payload) { }

    public EmailAddressDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(EmailAddress), aggregateId, payload) { }
}