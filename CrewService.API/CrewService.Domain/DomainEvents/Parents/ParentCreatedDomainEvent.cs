using CrewService.Domain.Models.Parents;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Parents;

public sealed record ParentCreatedDomainEvent : DomainEvent
{
    public ParentCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(Parent), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}
