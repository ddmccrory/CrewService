using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Seniority;

public sealed record CraftUpdatedDomainEvent : DomainEvent
{
    public CraftUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Craft), aggregateCtrlNbr.Value, payload) { }

    public CraftUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Craft), aggregateId, payload) { }
}