using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employment;

public sealed record EmploymentStatusHistoryCreatedDomainEvent : DomainEvent
{
    public EmploymentStatusHistoryCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(EmploymentStatusHistory), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}