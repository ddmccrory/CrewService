using CrewService.Domain.DomainEvents.UserAccess;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.UserAccess;

public sealed class UserParentAssignment : Entity
{
    public string UserId { get; private set; } = string.Empty;
    public ControlNumber ParentCtrlNbr { get; private set; }
    public string Role { get; private set; } = string.Empty;

    private UserParentAssignment()
    {
        ParentCtrlNbr = null!;
    }

    private UserParentAssignment(string userId, ControlNumber parentCtrlNbr, string role)
    {
        UserId = userId;
        ParentCtrlNbr = parentCtrlNbr;
        Role = role;
    }

    public static UserParentAssignment Create(string userId, ControlNumber parentCtrlNbr, string role)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(role);

        var assignment = new UserParentAssignment(
            userId,
            parentCtrlNbr,
            role);

        assignment.Raise(new UserParentAssignmentCreatedDomainEvent(assignment.CtrlNbr));

        return assignment;
    }

    public UserParentAssignment UpdateRole(string role)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);

        if (!string.Equals(Role, role, StringComparison.Ordinal))
        {
            Role = role;
            Raise(new UserParentAssignmentUpdatedDomainEvent(CtrlNbr, payload: new { Changes = new { role } }));
        }

        return this;
    }

    public void Delete()
    {
        Raise(new UserParentAssignmentDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}
