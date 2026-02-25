using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record PayrollTierCreatedDomainEvent : DomainEvent
{
    public PayrollTierCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(PayrollTier), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}
