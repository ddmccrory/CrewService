using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Boards;

public sealed class BoardCascadePolicy : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public string CascadeMode { get; private set; } = "UP_HIERARCHY";
    public int? MaxLevels { get; private set; }
    public bool AuxEnabled { get; private set; }
    public int? AuxMaxLevels { get; private set; }
    public string? SelectionStrategy { get; private set; }

    private BoardCascadePolicy() { WorkAreaGroupCtrlNbr = null!; CraftCtrlNbr = null!; }

    public static BoardCascadePolicy Create(ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr,
        string cascadeMode, int? maxLevels, bool auxEnabled, int? auxMaxLevels, string? selectionStrategy)
    {
        return new BoardCascadePolicy
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            CascadeMode = cascadeMode,
            MaxLevels = maxLevels,
            AuxEnabled = auxEnabled,
            AuxMaxLevels = auxMaxLevels,
            SelectionStrategy = selectionStrategy
        };
    }
}
