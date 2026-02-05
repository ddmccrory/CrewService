using CrewService.Domain.Models.Employees;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Employees
{
    public sealed record EmployeeDeletedDomainEvent : DomainEvent
    {
        public EmployeeDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
            : base(nameof(Employee), aggregateCtrlNbr.Value, payload) { }

        public EmployeeDeletedDomainEvent(long aggregateId, object? payload = null)
            : base(nameof(Employee), aggregateId, payload) { }
    }
}
