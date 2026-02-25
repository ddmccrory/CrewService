using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record PayrollTierDeletedDomainEvent : DomainEvent
{
    public PayrollTierDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(PayrollTier), aggregateCtrlNbr.Value, payload) { }

    public PayrollTierDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(PayrollTier), aggregateId, payload) { }
}
