using CrewService.Domain.DomainEvents.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Boards;

public sealed class RosterBoard : Entity
{
    private readonly List<RosterBoardPosition> _positions = [];

    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public ControlNumber RosterCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public BoardType BoardType { get; private set; }
    public RotationType RotationType { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyList<RosterBoardPosition> Positions => _positions.AsReadOnly();

    private RosterBoard()
    {
        WorkAreaGroupCtrlNbr = null!;
        CraftCtrlNbr = null!;
        RosterCtrlNbr = null!;
    }

    public static RosterBoard Create(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr,
        ControlNumber rosterCtrlNbr, string name,
        BoardType boardType = BoardType.Regular,
        RotationType rotationType = RotationType.StandardRotation,
        bool isActive = true)
    {
        var board = new RosterBoard
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            RosterCtrlNbr = rosterCtrlNbr,
            Name = name,
            BoardType = boardType,
            RotationType = rotationType,
            IsActive = isActive
        };
        board.Raise(new RosterBoardCreatedDomainEvent(board.CtrlNbr, name));
        return board;
    }

    public void Update(string name, BoardType boardType, RotationType rotationType, bool isActive)
    {
        Name = name;
        BoardType = boardType;
        RotationType = rotationType;
        IsActive = isActive;
    }

    public RosterBoardPosition AddPosition(ControlNumber employeeCtrlNbr, int positionOrder,
        ControlNumber staffablePositionCtrlNbr)
    {
        var position = RosterBoardPosition.Create(CtrlNbr, employeeCtrlNbr, positionOrder, staffablePositionCtrlNbr);
        _positions.Add(position);
        return position;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

public sealed class RosterBoardPosition : Entity
{
    public ControlNumber RosterBoardCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber StaffablePositionCtrlNbr { get; private set; }
    public int PositionOrder { get; private set; }
    public string HangoutStatus { get; private set; } = "Active";
    public DateTime? HangoutAtUtc { get; private set; }

    private RosterBoardPosition()
    {
        RosterBoardCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
        StaffablePositionCtrlNbr = null!;
    }

    internal static RosterBoardPosition Create(
        ControlNumber rosterBoardCtrlNbr, ControlNumber employeeCtrlNbr, int positionOrder,
        ControlNumber staffablePositionCtrlNbr)
    {
        return new RosterBoardPosition
        {
            RosterBoardCtrlNbr = rosterBoardCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            StaffablePositionCtrlNbr = staffablePositionCtrlNbr,
            PositionOrder = positionOrder
        };
    }

    public void Hangout()
    {
        HangoutStatus = "HungOut";
        HangoutAtUtc = DateTime.UtcNow;
        Raise(new PositionHungOutDomainEvent(CtrlNbr, EmployeeCtrlNbr));
    }

    public void MarkOff()
    {
        HangoutStatus = "MarkedOff";
        Raise(new PositionMarkedOffDomainEvent(CtrlNbr, EmployeeCtrlNbr));
    }

    public void RestoreFromHangout()
    {
        HangoutStatus = "Active";
        HangoutAtUtc = null;
        Raise(new PositionRestoredDomainEvent(CtrlNbr, EmployeeCtrlNbr));
    }
}
