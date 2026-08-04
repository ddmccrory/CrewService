using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Workflows;

public sealed record WorkflowTemplateCreatedDomainEvent : DomainEvent
{
    public WorkflowTemplateCreatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(WorkflowTemplate), aggregateCtrlNbr.Value, payload) { }
}
