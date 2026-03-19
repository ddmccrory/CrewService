using CrewService.Domain.DomainEvents.UserAccess;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.UserAccess;

public sealed class UserParentAssignment : Entity
{
    public string UserId { get; private set; } = string.Empty;
    public ControlNumber ParentCtrlNbr { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public ControlNumber? RailroadCtrlNbr { get; private set; }

    private UserParentAssignment()
    {
        ParentCtrlNbr = null!;
    }

    private UserParentAssignment(string userId, ControlNumber parentCtrlNbr, string role, ControlNumber? railroadCtrlNbr = null)
    {
        UserId = userId;
        ParentCtrlNbr = parentCtrlNbr;
        Role = role;
        RailroadCtrlNbr = railroadCtrlNbr;
    }

    public static UserParentAssignment Create(string userId, ControlNumber parentCtrlNbr, string role, ControlNumber? railroadCtrlNbr = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(role);

        var assignment = new UserParentAssignment(
            userId,
            parentCtrlNbr,
            role,
            railroadCtrlNbr);

        assignment.Raise(new UserParentAssignmentCreatedDomainEvent(assignment.CtrlNbr));

        return assignment;
    }

    public UserParentAssignment UpdateRole(string role, ControlNumber? railroadCtrlNbr = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);

        bool changed = !string.Equals(Role, role, StringComparison.Ordinal)
                     || RailroadCtrlNbr != railroadCtrlNbr;

        if (changed)
        {
            Role = role;
            RailroadCtrlNbr = railroadCtrlNbr;
            Raise(new UserParentAssignmentUpdatedDomainEvent(CtrlNbr, payload: new { Changes = new { role, railroadCtrlNbr } }));
        }

        return this;
    }

    public void Delete()
    {
        Raise(new UserParentAssignmentDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}
