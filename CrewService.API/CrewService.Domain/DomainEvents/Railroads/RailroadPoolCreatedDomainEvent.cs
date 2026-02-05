using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadPoolCreatedDomainEvent : DomainEvent
{
    public RailroadPoolCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(RailroadPool), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}