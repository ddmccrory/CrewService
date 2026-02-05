using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record EmployeePriorServiceCreditCreatedDomainEvent : DomainEvent
{
    public EmployeePriorServiceCreditCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(EmployeePriorServiceCredit), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}