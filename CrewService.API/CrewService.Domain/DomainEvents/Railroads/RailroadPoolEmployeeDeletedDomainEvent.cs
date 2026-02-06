using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadPoolEmployeeDeletedDomainEvent : DomainEvent
{
    public RailroadPoolEmployeeDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(RailroadPoolEmployee), aggregateCtrlNbr.Value, payload) { }

    public RailroadPoolEmployeeDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(RailroadPoolEmployee), aggregateId, payload) { }
}