using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadPoolPayrollTierCreatedDomainEvent : DomainEvent
{
    public RailroadPoolPayrollTierCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(RailroadPoolPayrollTier), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}