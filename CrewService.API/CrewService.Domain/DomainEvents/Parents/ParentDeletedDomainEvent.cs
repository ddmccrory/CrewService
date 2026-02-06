using CrewService.Domain.Models.Parents;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Parents;

public sealed record ParentDeletedDomainEvent : DomainEvent
{
    public ParentDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Parent), aggregateCtrlNbr.Value, payload) { }

    public ParentDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Parent), aggregateId, payload) { }
}