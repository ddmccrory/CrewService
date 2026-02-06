using CrewService.Domain.Models.Employment;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employment;

public sealed record EmploymentStatusHistoryDeletedDomainEvent : DomainEvent
{
    public EmploymentStatusHistoryDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(EmploymentStatusHistory), aggregateCtrlNbr.Value, payload) { }

    public EmploymentStatusHistoryDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(EmploymentStatusHistory), aggregateId, payload) { }
}