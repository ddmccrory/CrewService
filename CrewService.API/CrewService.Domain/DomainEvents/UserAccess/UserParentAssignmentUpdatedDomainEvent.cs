using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.UserAccess;

public sealed record UserParentAssignmentUpdatedDomainEvent : DomainEvent
{
    public UserParentAssignmentUpdatedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(UserParentAssignment), aggregateCtrlNbr.Value, payload) { }
}
