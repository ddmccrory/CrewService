using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Safety;

public sealed class SafetyObservation : Entity
{
    private readonly List<SafetyObservationAction> _actions = [];

    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public ControlNumber ObserverEmployeeCtrlNbr { get; private set; }
    public string CategoryCode { get; private set; } = string.Empty;
    public string AreaCode { get; private set; } = string.Empty;
    public string? SubdivisionCode { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime ObservedAtUtc { get; private set; }
    public string Status { get; private set; } = "Open";

    public IReadOnlyList<SafetyObservationAction> Actions => _actions.AsReadOnly();

    private SafetyObservation()
    {
        WorkAreaGroupCtrlNbr = null!;
        ObserverEmployeeCtrlNbr = null!;
    }

    public static SafetyObservation Create(
        long workAreaGroupCtrlNbr, long observerEmployeeCtrlNbr,
        string categoryCode, string areaCode, string description,
        string? subdivisionCode = null)
    {
        var obs = new SafetyObservation
        {
            WorkAreaGroupCtrlNbr = ControlNumber.Create(workAreaGroupCtrlNbr),
            ObserverEmployeeCtrlNbr = ControlNumber.Create(observerEmployeeCtrlNbr),
            CategoryCode = categoryCode,
            AreaCode = areaCode,
            SubdivisionCode = subdivisionCode,
            Description = description,
            ObservedAtUtc = DateTime.UtcNow
        };
        obs.Raise(new SafetyObservationCreatedDomainEvent(obs));
        return obs;
    }

    public SafetyObservationAction AddAction(ControlNumber takenByCtrlNbr, string actionDescription)
    {
        if (Status == "Resolved")
            throw new InvalidOperationException("Cannot add actions to a resolved observation.");

        var action = SafetyObservationAction.Create(CtrlNbr, takenByCtrlNbr, actionDescription);
        _actions.Add(action);

        if (Status == "Open")
            Status = "ActionTaken";

        return action;
    }
}

public sealed class SafetyObservationAction : Entity
{
    public ControlNumber ObservationCtrlNbr { get; private set; }
    public string ActionDescription { get; private set; } = string.Empty;
    public ControlNumber TakenByCtrlNbr { get; private set; }
    public DateTime TakenAtUtc { get; private set; }

    private SafetyObservationAction()
    {
        ObservationCtrlNbr = null!;
        TakenByCtrlNbr = null!;
    }

    internal static SafetyObservationAction Create(
        ControlNumber observationCtrlNbr, ControlNumber takenByCtrlNbr, string actionDescription)
    {
        return new SafetyObservationAction
        {
            ObservationCtrlNbr = observationCtrlNbr,
            TakenByCtrlNbr = takenByCtrlNbr,
            ActionDescription = actionDescription,
            TakenAtUtc = DateTime.UtcNow
        };
    }
}

public sealed record SafetyObservationCreatedDomainEvent : DomainEvent
{
    public SafetyObservationCreatedDomainEvent(SafetyObservation obs)
        : base(nameof(SafetyObservation), obs.CtrlNbr.Value,
            new { obs.CategoryCode, obs.AreaCode, obs.Description }) { }
}
