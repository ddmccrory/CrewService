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
        return new SeniorityState(
            stateDescription,
            active,
            cutBack,
            inactive);
    }

    public void Update(string stateDescription, bool active, bool cutBack, bool inactive)
    {
        StateDescription = stateDescription;
        Active = active;
        CutBack = cutBack;
        Inactive = inactive;
    }
}