using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employment;

public sealed record EmploymentStatusDeletedDomainEvent : DomainEvent
{
    public EmploymentStatusDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(EmploymentStatus), aggregateCtrlNbr.Value, payload) { }

    public EmploymentStatusDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(EmploymentStatus), aggregateId, payload) { }
}