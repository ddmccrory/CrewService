using CrewService.Domain.Models.Parents;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Parents;

public sealed record ParentUpdatedDomainEvent : DomainEvent
{
    public ParentUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Parent), aggregateCtrlNbr.Value, payload) { }

    public ParentUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Parent), aggregateId, payload) { }
}