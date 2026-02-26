using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.UserAccess;

public sealed record UserParentAssignmentDeletedDomainEvent : DomainEvent
{
    public UserParentAssignmentDeletedDomainEvent(ControlNumber aggregateCtrlNbr, object? payload = null)
        : base(nameof(UserParentAssignment), aggregateCtrlNbr.Value, payload) { }
}
