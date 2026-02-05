using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadDeletedDomainEvent : DomainEvent
{
    public RailroadDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Railroad), aggregateCtrlNbr.Value, payload) { }

    public RailroadDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Railroad), aggregateId, payload) { }
}
