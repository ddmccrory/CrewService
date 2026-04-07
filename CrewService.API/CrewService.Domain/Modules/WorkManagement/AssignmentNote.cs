using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class AssignmentNote : Entity
{
    public ControlNumber ShiftInstanceCtrlNbr { get; private set; }
    public ControlNumber AssignmentCtrlNbr { get; private set; }
    public string NoteText { get; private set; } = string.Empty;

    private AssignmentNote()
    {
        ShiftInstanceCtrlNbr = null!;
        AssignmentCtrlNbr = null!;
    }

    internal static AssignmentNote Create(
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber assignmentCtrlNbr,
        string noteText)
    {
        return new AssignmentNote
        {
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            AssignmentCtrlNbr = assignmentCtrlNbr,
            NoteText = noteText
        };
    }

    internal void UpdateText(string noteText)
    {
        NoteText = noteText;
    }
}
