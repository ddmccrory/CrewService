using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadPoolUpdatedDomainEvent : DomainEvent
{
    public RailroadPoolUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(RailroadPool), aggregateCtrlNbr.Value, payload) { }

    public RailroadPoolUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(RailroadPool), aggregateId, payload) { }
}