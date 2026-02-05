using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record EmployeePriorServiceCreditDeletedDomainEvent : DomainEvent
{
    public EmployeePriorServiceCreditDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(EmployeePriorServiceCredit), aggregateCtrlNbr.Value, payload) { }

    public EmployeePriorServiceCreditDeletedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(EmployeePriorServiceCredit), aggregateId, payload) { }
}