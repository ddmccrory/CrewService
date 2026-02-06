using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record EmployeeCreatedDomainEvent : DomainEvent
{
    public EmployeeCreatedDomainEvent(ControlNumber aggregateCtrlNbr)
        : base(nameof(Employee), aggregateCtrlNbr.Value, payload: new { AggregateCtrlNbr = aggregateCtrlNbr.Value }) { }
}