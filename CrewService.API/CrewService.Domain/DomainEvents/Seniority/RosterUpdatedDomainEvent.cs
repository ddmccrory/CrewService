using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Seniority;

public sealed record RosterUpdatedDomainEvent : DomainEvent
{
    public RosterUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Roster), aggregateCtrlNbr.Value, payload) { }

    public RosterUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Roster), aggregateId, payload) { }
}