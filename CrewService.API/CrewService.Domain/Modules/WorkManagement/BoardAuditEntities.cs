using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class BoardSnapshot : Entity
{
    private readonly List<BoardSnapshotRow> _rows = [];

    public ControlNumber ShiftInstanceCtrlNbr { get; private set; }
    public ControlNumber? PositionSlotInstanceCtrlNbr { get; private set; }
    public ControlNumber? VacancyImpactCtrlNbr { get; private set; }
    public DateTime CapturedAtUtc { get; private set; }
    public string TriggerSource { get; private set; } = string.Empty;
    public int DecisionSequence { get; private set; }
    public IReadOnlyList<BoardSnapshotRow> Rows => _rows.AsReadOnly();

    private BoardSnapshot()
    {
        ShiftInstanceCtrlNbr = null!;
    }

    public static BoardSnapshot Create(
        ControlNumber shiftInstanceCtrlNbr,
        DateTime capturedAtUtc,
        string triggerSource,
        int decisionSequence,
        ControlNumber? positionSlotInstanceCtrlNbr = null,
        ControlNumber? vacancyImpactCtrlNbr = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerSource);

        return new BoardSnapshot
        {
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            PositionSlotInstanceCtrlNbr = positionSlotInstanceCtrlNbr,
            VacancyImpactCtrlNbr = vacancyImpactCtrlNbr,
            CapturedAtUtc = capturedAtUtc,
            TriggerSource = triggerSource,
            DecisionSequence = decisionSequence
        };
    }

    public void AddRow(BoardSnapshotRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        _rows.Add(row);
    }
}

public sealed class BoardSnapshotRow : Entity
{
    public ControlNumber BoardSnapshotCtrlNbr { get; private set; }
    public ControlNumber BoardSlotInstanceCtrlNbr { get; private set; }
    public ControlNumber ShiftInstanceCtrlNbr { get; private set; }
    public ControlNumber RosterBoardCtrlNbr { get; private set; }
    public ControlNumber? RosterBoardPositionCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public int BoardOrder { get; private set; }
    public long CallSequence { get; private set; }
    public DateTime? TieUpAtUtc { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string BoardName { get; private set; } = string.Empty;
    public string EmployeeName { get; private set; } = string.Empty;
    public string PositionName { get; private set; } = string.Empty;

    private BoardSnapshotRow()
    {
        BoardSnapshotCtrlNbr = null!;
        BoardSlotInstanceCtrlNbr = null!;
        ShiftInstanceCtrlNbr = null!;
        RosterBoardCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static BoardSnapshotRow Create(
        ControlNumber boardSnapshotCtrlNbr,
        ControlNumber boardSlotInstanceCtrlNbr,
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber rosterBoardCtrlNbr,
        ControlNumber employeeCtrlNbr,
        int boardOrder,
        long callSequence,
        DateTime? tieUpAtUtc,
        string status,
        string boardName,
        string employeeName,
        string positionName,
        ControlNumber? rosterBoardPositionCtrlNbr = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentException.ThrowIfNullOrWhiteSpace(boardName);

        return new BoardSnapshotRow
        {
            BoardSnapshotCtrlNbr = boardSnapshotCtrlNbr,
            BoardSlotInstanceCtrlNbr = boardSlotInstanceCtrlNbr,
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            RosterBoardCtrlNbr = rosterBoardCtrlNbr,
            RosterBoardPositionCtrlNbr = rosterBoardPositionCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            BoardOrder = boardOrder,
            CallSequence = callSequence,
            TieUpAtUtc = tieUpAtUtc,
            Status = status,
            BoardName = boardName,
            EmployeeName = employeeName,
            PositionName = positionName
        };
    }
}

public sealed class BoardSelectionDecision : Entity
{
    public ControlNumber ShiftInstanceCtrlNbr { get; private set; }
    public ControlNumber PositionSlotInstanceCtrlNbr { get; private set; }
    public ControlNumber? VacancyImpactCtrlNbr { get; private set; }
    public ControlNumber? SnapshotCtrlNbr { get; private set; }
    public ControlNumber? SelectedBoardSlotInstanceCtrlNbr { get; private set; }
    public ControlNumber? SelectedEmployeeCtrlNbr { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public int DecisionSequence { get; private set; }
    public string DecisionSource { get; private set; } = string.Empty;
    public string DecisionPhase { get; private set; } = string.Empty;
    public string? DecisionJson { get; private set; }

    private BoardSelectionDecision()
    {
        ShiftInstanceCtrlNbr = null!;
        PositionSlotInstanceCtrlNbr = null!;
    }

    public static BoardSelectionDecision Create(
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber positionSlotInstanceCtrlNbr,
        DateTime occurredAtUtc,
        int decisionSequence,
        string decisionSource,
        string decisionPhase,
        ControlNumber? vacancyImpactCtrlNbr = null,
        ControlNumber? snapshotCtrlNbr = null,
        ControlNumber? selectedBoardSlotInstanceCtrlNbr = null,
        ControlNumber? selectedEmployeeCtrlNbr = null,
        string? decisionJson = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionPhase);

        return new BoardSelectionDecision
        {
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            PositionSlotInstanceCtrlNbr = positionSlotInstanceCtrlNbr,
            VacancyImpactCtrlNbr = vacancyImpactCtrlNbr,
            SnapshotCtrlNbr = snapshotCtrlNbr,
            SelectedBoardSlotInstanceCtrlNbr = selectedBoardSlotInstanceCtrlNbr,
            SelectedEmployeeCtrlNbr = selectedEmployeeCtrlNbr,
            OccurredAtUtc = occurredAtUtc,
            DecisionSequence = decisionSequence,
            DecisionSource = decisionSource,
            DecisionPhase = decisionPhase,
            DecisionJson = decisionJson
        };
    }
}
