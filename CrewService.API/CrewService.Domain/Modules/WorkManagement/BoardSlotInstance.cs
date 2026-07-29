using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class BoardSlotInstance : Entity
{
    public ControlNumber ShiftInstanceCtrlNbr { get; private set; }
    public ControlNumber RosterBoardCtrlNbr { get; private set; }
    public ControlNumber? RosterBoardPositionCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public int BoardOrder { get; private set; }
    public long CallSequence { get; private set; }
    public BoardSlotStatus Status { get; private set; } = BoardSlotStatus.Available;

    // Stamped / denormalized from board config
    public string BoardName { get; private set; } = string.Empty;
    public string EmployeeName { get; private set; } = string.Empty;
    public string PositionName { get; private set; } = string.Empty;

    // Operational tracking
    public int DaysWorked { get; private set; }
    public int ConsecutiveDays { get; private set; }
    public DateTime? RestAvailableAtUtc { get; private set; }
    public DateTime? TieUpAtUtc { get; private set; }

    private BoardSlotInstance()
    {
        ShiftInstanceCtrlNbr = null!;
        RosterBoardCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    internal static BoardSlotInstance Create(
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber rosterBoardCtrlNbr,
        ControlNumber? rosterBoardPositionCtrlNbr,
        ControlNumber employeeCtrlNbr,
        int boardOrder,
        long callSequence,
        string boardName,
        string employeeName,
        string positionName = "",
        int daysWorked = 0,
        int consecutiveDays = 0,
        DateTime? restAvailableAtUtc = null,
        DateTime? tieUpAtUtc = null)
    {
        return new BoardSlotInstance
        {
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            RosterBoardCtrlNbr = rosterBoardCtrlNbr,
            RosterBoardPositionCtrlNbr = rosterBoardPositionCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            BoardOrder = boardOrder,
            CallSequence = callSequence,
            BoardName = boardName,
            EmployeeName = employeeName,
            PositionName = positionName,
            DaysWorked = daysWorked,
            ConsecutiveDays = consecutiveDays,
            RestAvailableAtUtc = restAvailableAtUtc,
            TieUpAtUtc = tieUpAtUtc
        };
    }

    public void Call()
    {
        Status = BoardSlotStatus.Called;
    }

    public void MarkOnDuty()
    {
        Status = BoardSlotStatus.OnDuty;
    }

    public void MarkTiedUp(long newCallSequence)
    {
        Status = BoardSlotStatus.TiedUp;
        CallSequence = newCallSequence;
        TieUpAtUtc = DateTime.UtcNow;
    }

    public void Reposition(int newBoardOrder)
    {
        BoardOrder = newBoardOrder;
        if (Status == BoardSlotStatus.TiedUp)
        {
            Status = BoardSlotStatus.Available;
        }
    }

    public void Hangout()
    {
        Status = BoardSlotStatus.HungOut;
    }

    public void MarkOff()
    {
        Status = BoardSlotStatus.MarkedOff;
    }

    public void RestoreToAvailable()
    {
        Status = BoardSlotStatus.Available;
    }

    public void MarkUnavailable()
    {
        Status = BoardSlotStatus.Unavailable;
    }

    public void UpdateOperationalTracking(int daysWorked, int consecutiveDays, DateTime? restAvailableAtUtc)
    {
        DaysWorked = daysWorked;
        ConsecutiveDays = consecutiveDays;
        RestAvailableAtUtc = restAvailableAtUtc;
    }
}
