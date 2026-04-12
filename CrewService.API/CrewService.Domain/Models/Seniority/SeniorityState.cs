using CrewService.Domain.DomainEvents.Seniority;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Seniority;

public sealed class SeniorityState : Entity
{
    public string StateDescription { get; private set; } = string.Empty;
    public StateType StateType { get; private set; }
    public ControlNumber ParentCtrlNbr { get; private set; } = null!;

    private SeniorityState() { }

    private SeniorityState(
        string stateDescription,
        StateType stateType,
        ControlNumber parentCtrlNbr)
    {
        StateDescription = stateDescription;
        StateType = stateType;
        ParentCtrlNbr = parentCtrlNbr;
    }

    public static SeniorityState Create(
        string stateDescription,
        StateType stateType,
        long parentCtrlNbr)
    {
        var entity = new SeniorityState(
            stateDescription,
            stateType,
            parentCtrlNbr);
        entity.Raise(new SeniorityStateCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public void Update(string stateDescription, StateType stateType)
    {
        var changes = new Dictionary<string, object?>();

        if (StateDescription != stateDescription) { StateDescription = stateDescription; changes["stateDescription"] = stateDescription; }
        if (StateType != stateType) { StateType = stateType; changes["stateType"] = stateType; }

        if (changes.Count > 0)
        {
            Raise(new SeniorityStateUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }
    }

    public void Delete()
    {
        Raise(new SeniorityStateDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}