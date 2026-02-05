using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record EmployeePriorServiceCreditUpdatedDomainEvent : DomainEvent
{
    public EmployeePriorServiceCreditUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(EmployeePriorServiceCredit), aggregateCtrlNbr.Value, payload) { }

    public EmployeePriorServiceCreditUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(EmployeePriorServiceCredit), aggregateId, payload) { }
}