using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadUpdatedDomainEvent : DomainEvent
{
    public RailroadUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Railroad), aggregateCtrlNbr.Value, payload) { }

    public RailroadUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Railroad), aggregateId, payload) { }
}
