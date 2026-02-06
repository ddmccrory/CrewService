using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadEmployeeDeletedDomainEvent : DomainEvent
{
    public RailroadEmployeeDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(RailroadEmployee), aggregateCtrlNbr.Value, payload) { }

    public RailroadEmployeeDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(RailroadEmployee), aggregateId, payload) { }
}