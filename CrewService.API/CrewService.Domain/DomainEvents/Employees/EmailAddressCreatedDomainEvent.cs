using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record EmailAddressCreatedDomainEvent : DomainEvent
{
    public EmailAddressCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(EmailAddress), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}