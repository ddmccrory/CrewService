using CrewService.Domain.ValueObjects;
using SeniorityEntity = CrewService.Domain.Models.Seniority.Seniority;

namespace CrewService.Domain.DomainEvents.Seniority;

public sealed record SeniorityCreatedDomainEvent : DomainEvent
{
    public SeniorityCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(SeniorityEntity), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}