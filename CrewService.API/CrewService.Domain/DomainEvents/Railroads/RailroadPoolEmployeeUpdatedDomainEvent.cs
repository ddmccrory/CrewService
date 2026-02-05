using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadPoolEmployeeUpdatedDomainEvent : DomainEvent
{
    public RailroadPoolEmployeeUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(RailroadPoolEmployee), aggregateCtrlNbr.Value, payload) { }

    public RailroadPoolEmployeeUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(RailroadPoolEmployee), aggregateId, payload) { }
}