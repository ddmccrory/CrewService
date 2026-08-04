using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Workflows;

public sealed record WorkflowVersionCreatedDomainEvent : DomainEvent
{
    public WorkflowVersionCreatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(WorkflowVersion), aggregateCtrlNbr.Value, payload) { }
}
