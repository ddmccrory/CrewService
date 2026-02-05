using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees;

public sealed record EmployeeUpdatedDomainEvent : DomainEvent
{
    public EmployeeUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(Employee), aggregateCtrlNbr.Value, payload) { }

    public EmployeeUpdatedDomainEvent(long aggregateId, object? payload = null)
        : base(nameof(Employee), aggregateId, payload) { }
}
