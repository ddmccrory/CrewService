using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Seniority;

public sealed record RosterDeletedDomainEvent : DomainEvent
{
    public RosterDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Roster), aggregateCtrlNbr.Value, payload) { }

    public RosterDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Roster), aggregateId, payload) { }
}