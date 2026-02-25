using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Railroads;

public sealed record PayrollTierUpdatedDomainEvent : DomainEvent
{
    public PayrollTierUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(PayrollTier), aggregateCtrlNbr.Value, payload) { }

    public PayrollTierUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(PayrollTier), aggregateId, payload) { }
}
