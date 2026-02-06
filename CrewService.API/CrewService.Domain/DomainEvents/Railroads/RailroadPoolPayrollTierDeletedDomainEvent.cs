using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record RailroadPoolPayrollTierDeletedDomainEvent : DomainEvent
{
    public RailroadPoolPayrollTierDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(RailroadPoolPayrollTier), aggregateCtrlNbr.Value, payload) { }

    public RailroadPoolPayrollTierDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(RailroadPoolPayrollTier), aggregateId, payload) { }
}