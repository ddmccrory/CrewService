using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Seniority;

public sealed record RosterCreatedDomainEvent : DomainEvent
{
    public RosterCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(Roster), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}