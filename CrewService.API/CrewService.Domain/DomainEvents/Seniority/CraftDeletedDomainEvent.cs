using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Seniority;

public sealed record CraftDeletedDomainEvent : DomainEvent
{
    public CraftDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Craft), aggregateCtrlNbr.Value, payload) { }

    public CraftDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Craft), aggregateId, payload) { }
}