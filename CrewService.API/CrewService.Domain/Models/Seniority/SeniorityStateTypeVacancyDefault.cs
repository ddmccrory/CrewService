using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Seniority;

/// <summary>
/// Railroad-level default vacancy action by seniority state type.
/// Used when a specific state does not have an explicit vacancy config.
/// </summary>
public sealed class SeniorityStateTypeVacancyDefault : Entity
{
    public ControlNumber ParentCtrlNbr { get; private set; } = null!;
    public ControlNumber RailroadCtrlNbr { get; private set; } = null!;
    public StateType StateType { get; private set; }
    public VacancyAction DefaultVacancyAction { get; private set; }

    private SeniorityStateTypeVacancyDefault() { }

    public static SeniorityStateTypeVacancyDefault Create(
        ControlNumber parentCtrlNbr,
        ControlNumber railroadCtrlNbr,
        StateType stateType,
        VacancyAction defaultVacancyAction)
    {
        Validate(stateType, defaultVacancyAction);

        return new SeniorityStateTypeVacancyDefault
        {
            ParentCtrlNbr = parentCtrlNbr,
            RailroadCtrlNbr = railroadCtrlNbr,
            StateType = stateType,
            DefaultVacancyAction = defaultVacancyAction
        };
    }

    public void Update(VacancyAction defaultVacancyAction)
    {
        Validate(StateType, defaultVacancyAction);
        DefaultVacancyAction = defaultVacancyAction;
    }

    private static void Validate(StateType stateType, VacancyAction action)
    {
        if (stateType == StateType.OffProperty && action == VacancyAction.LeaveOnCurrentPosition)
            throw new ArgumentException("LeaveOnCurrentPosition is not allowed for OffProperty state type.", nameof(action));

        if (action == VacancyAction.MoveToBoard)
            throw new ArgumentException("MoveToBoard is not allowed as a state-type default because board type is state-specific.", nameof(action));
    }
}
