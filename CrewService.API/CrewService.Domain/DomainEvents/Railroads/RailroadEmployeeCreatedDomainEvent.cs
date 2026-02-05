using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadEmployeeCreatedDomainEvent : DomainEvent
{
    public RailroadEmployeeCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(RailroadEmployee), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}
