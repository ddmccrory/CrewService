using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadPoolDeletedDomainEvent : DomainEvent
{
    public RailroadPoolDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(RailroadPool), aggregateCtrlNbr.Value, payload) { }

    public RailroadPoolDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(RailroadPool), aggregateId, payload) { }
}