using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Seniority;

/// <summary>
/// Railroad-level configuration that maps a SeniorityState to the vacancy action
/// the system should take when an employee's seniority state is changed to that state.
/// One row per SeniorityState per railroad.
/// </summary>
public sealed class SeniorityStateVacancyConfig : Entity
{
    public ControlNumber ParentCtrlNbr { get; private set; } = null!;

    /// <summary>
    /// The railroad this configuration applies to.
    /// </summary>
    public ControlNumber RailroadCtrlNbr { get; private set; } = null!;

    /// <summary>
    /// The SeniorityState this action applies to.
    /// </summary>
    public ControlNumber SeniorityStateCtrlNbr { get; private set; } = null!;

    /// <summary>
    /// What should happen to the employee's current position when they transition into this state.
    /// </summary>
    public VacancyAction VacancyAction { get; private set; }

    /// <summary>
    /// Required when <see cref="VacancyAction"/> is <see cref="VacancyAction.MoveToBoard"/>.
    /// The type of board the employee should be placed on. The actual board is resolved
    /// at runtime by matching this type with the employee's craft.
    /// </summary>
    public BoardType? TargetBoardType { get; private set; }

    private SeniorityStateVacancyConfig() { }

    public static SeniorityStateVacancyConfig Create(
        ControlNumber parentCtrlNbr,
        ControlNumber railroadCtrlNbr,
        ControlNumber seniorityStateCtrlNbr,
        VacancyAction vacancyAction,
        BoardType? targetBoardType = null)
    {
        if (vacancyAction == VacancyAction.MoveToBoard && targetBoardType is null)
            throw new ArgumentException("A target board type must be specified when the vacancy action is MoveToBoard.", nameof(targetBoardType));

        return new SeniorityStateVacancyConfig
        {
            ParentCtrlNbr = parentCtrlNbr,
            RailroadCtrlNbr = railroadCtrlNbr,
            SeniorityStateCtrlNbr = seniorityStateCtrlNbr,
            VacancyAction = vacancyAction,
            TargetBoardType = vacancyAction == VacancyAction.MoveToBoard ? targetBoardType : null
        };
    }

    public void Update(VacancyAction vacancyAction, BoardType? targetBoardType)
    {
        if (vacancyAction == VacancyAction.MoveToBoard && targetBoardType is null)
            throw new ArgumentException("A target board type must be specified when the vacancy action is MoveToBoard.", nameof(targetBoardType));

        VacancyAction = vacancyAction;
        TargetBoardType = vacancyAction == VacancyAction.MoveToBoard ? targetBoardType : null;
    }
}
