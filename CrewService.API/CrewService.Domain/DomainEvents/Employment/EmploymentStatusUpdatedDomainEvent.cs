using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employment;

public sealed record EmploymentStatusUpdatedDomainEvent : DomainEvent
{
    public EmploymentStatusUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(EmploymentStatus), aggregateCtrlNbr.Value, payload) { }

    public EmploymentStatusUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(EmploymentStatus), aggregateId, payload) { }
}