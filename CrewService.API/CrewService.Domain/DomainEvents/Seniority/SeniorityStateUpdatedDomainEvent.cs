using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Seniority;

public sealed record SeniorityStateUpdatedDomainEvent : DomainEvent
{
    public SeniorityStateUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(SeniorityState), aggregateCtrlNbr.Value, payload) { }

    public SeniorityStateUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(SeniorityState), aggregateId, payload) { }
}