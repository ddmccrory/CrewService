using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employment;

public sealed record EmploymentStatusCreatedDomainEvent : DomainEvent
{
    public EmploymentStatusCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(EmploymentStatus), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}