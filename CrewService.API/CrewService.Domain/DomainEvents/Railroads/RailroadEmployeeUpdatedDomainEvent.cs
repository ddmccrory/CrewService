using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadEmployeeUpdatedDomainEvent : DomainEvent
{
    public RailroadEmployeeUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(RailroadEmployee), aggregateCtrlNbr.Value, payload) { }

    public RailroadEmployeeUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(RailroadEmployee), aggregateId, payload) { }
}