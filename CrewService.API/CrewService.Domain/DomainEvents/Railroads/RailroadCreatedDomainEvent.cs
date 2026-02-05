using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadCreatedDomainEvent : DomainEvent
{
    public RailroadCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(Railroad), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}
