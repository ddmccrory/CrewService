using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Seniority;

public sealed record SeniorityStateCreatedDomainEvent : DomainEvent
{
    public SeniorityStateCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(SeniorityState), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}