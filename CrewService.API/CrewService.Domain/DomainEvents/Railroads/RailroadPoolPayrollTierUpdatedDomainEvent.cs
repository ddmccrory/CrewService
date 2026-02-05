using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadPoolPayrollTierUpdatedDomainEvent : DomainEvent
{
    public RailroadPoolPayrollTierUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(RailroadPoolPayrollTier), aggregateCtrlNbr.Value, payload) { }

    public RailroadPoolPayrollTierUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(RailroadPoolPayrollTier), aggregateId, payload) { }
}