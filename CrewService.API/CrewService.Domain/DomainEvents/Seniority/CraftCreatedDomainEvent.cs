using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Seniority;

public sealed record CraftCreatedDomainEvent : DomainEvent
{
    public CraftCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(Craft), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}