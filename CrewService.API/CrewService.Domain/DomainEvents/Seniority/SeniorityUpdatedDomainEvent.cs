using CrewService.Domain.ValueObjects;
using SeniorityEntity = CrewService.Domain.Models.Seniority.Seniority;

namespace CrewService.Domain.DomainEvents.Seniority;

public sealed record SeniorityUpdatedDomainEvent : DomainEvent
{
    public SeniorityUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(SeniorityEntity), aggregateCtrlNbr.Value, payload) { }

    public SeniorityUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(SeniorityEntity), aggregateId, payload) { }
}