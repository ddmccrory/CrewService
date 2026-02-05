using CrewService.Domain.DomainEvents.Seniority;
using CrewService.Domain.Primitives;

namespace CrewService.Domain.Models.Seniority;

public sealed class SeniorityState : Entity
{
    public string StateDescription { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public bool CutBack { get; private set; }
    public bool Inactive { get; private set; }

    private SeniorityState() { }

    private SeniorityState(
        string stateDescription,
        bool active,
        bool cutBack,
        bool inactive)
    {
        StateDescription = stateDescription;
        Active = active;
        CutBack = cutBack;
        Inactive = inactive;
    }

    public static SeniorityState Create(
        string stateDescription,
        bool active,
        bool cutBack,
        bool inactive)
    {
        var entity = new SeniorityState(
            stateDescription,
            active,
            cutBack,
            inactive);
        entity.Raise(new SeniorityStateCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public void Update(string stateDescription, bool active, bool cutBack, bool inactive)
    {
        var changes = new Dictionary<string, object?>();

        if (StateDescription != stateDescription) { StateDescription = stateDescription; changes["stateDescription"] = stateDescription; }
        if (Active != active) { Active = active; changes["active"] = active; }
        if (CutBack != cutBack) { CutBack = cutBack; changes["cutBack"] = cutBack; }
        if (Inactive != inactive) { Inactive = inactive; changes["inactive"] = inactive; }

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