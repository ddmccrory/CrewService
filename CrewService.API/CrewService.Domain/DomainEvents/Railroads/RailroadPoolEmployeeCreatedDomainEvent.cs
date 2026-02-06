using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadPoolEmployeeCreatedDomainEvent : DomainEvent
{
    public RailroadPoolEmployeeCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(RailroadPoolEmployee), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}