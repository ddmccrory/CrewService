using CrewService.Domain.DomainEvents.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Boards;

public sealed class RosterBoard : Entity
{
    private readonly List<RosterBoardPosition> _positions = [];

    public ControlNumber CraftCtrlNbr { get; private set; }
    public ControlNumber RosterCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public BoardType BoardType { get; private set; }
    public RotationType RotationType { get; private set; }
    public bool IsActive { get; private set; }

    public int RequiredPositions { get; private set; }
    /// <summary>Board-level strategy override. Null = inherit from craft's assigned strategy.</summary>
    public ControlNumber? RequiredPositionsStrategyCtrlNbr { get; private set; }

    public IReadOnlyList<RosterBoardPosition> Positions => _positions.AsReadOnly();

    private RosterBoard()
    {
        CraftCtrlNbr = null!;
        RosterCtrlNbr = null!;
    }

    public static RosterBoard Create(
        ControlNumber craftCtrlNbr,
        ControlNumber rosterCtrlNbr, string name,
        BoardType boardType = BoardType.ExtraBoard,
        RotationType rotationType = RotationType.StandardRotation,
        bool isActive = true,
        int requiredPositions = 0)
    {
        // Hangout and ExtendedAbsence boards have no rotation
        if (boardType is BoardType.Hangout or BoardType.ExtendedAbsence or BoardType.NewHire)
            rotationType = RotationType.None;

        var board = new RosterBoard
        {
            CraftCtrlNbr = craftCtrlNbr,
            RosterCtrlNbr = rosterCtrlNbr,
            Name = name,
            BoardType = boardType,
            RotationType = rotationType,
            IsActive = isActive,
            RequiredPositions = requiredPositions
        };
        board.Raise(new RosterBoardCreatedDomainEvent(board.CtrlNbr, name));
        return board;
    }

    public void Update(string name, BoardType boardType, RotationType rotationType, bool isActive, int requiredPositions = 0)
    {
        // Hangout and ExtendedAbsence boards have no rotation
        if (boardType is BoardType.Hangout or BoardType.ExtendedAbsence or BoardType.NewHire)
            rotationType = RotationType.None;

        Name = name;
        BoardType = boardType;
        RotationType = rotationType;
        IsActive = isActive;
        RequiredPositions = requiredPositions;
    }

    public void UpdateRequiredPositions(int value) => RequiredPositions = value;

    public void SetRequiredPositionsStrategy(ControlNumber? strategyCtrlNbr) =>
        RequiredPositionsStrategyCtrlNbr = strategyCtrlNbr;


    public RosterBoardPosition AddPosition(ControlNumber employeeCtrlNbr, int positionOrder,
        ControlNumber staffablePositionCtrlNbr)
    {
        if (_positions.Any(p => p.EmployeeCtrlNbr == employeeCtrlNbr))
            throw new InvalidOperationException(
                $"Employee {employeeCtrlNbr} already has a position on board {CtrlNbr}. An employee can only have one position per board.");

        var position = RosterBoardPosition.Create(CtrlNbr, employeeCtrlNbr, positionOrder, staffablePositionCtrlNbr);
        _positions.Add(position);
        return position;
    }

    public void RemovePosition(RosterBoardPosition position)
    {
        _positions.Remove(position);
    }

    public void ReorderPositions(IReadOnlyList<(ControlNumber PositionCtrlNbr, int NewOrder)> ordering)
    {
        var beforeState = _positions
            .ToDictionary(p => p.CtrlNbr.Value, p => new { p.EmployeeCtrlNbr, PreviousOrder = p.PositionOrder });

        foreach (var (positionCtrlNbr, newOrder) in ordering)
        {
            var position = _positions.FirstOrDefault(p => p.CtrlNbr == positionCtrlNbr)
                ?? throw new InvalidOperationException($"Position {positionCtrlNbr} not found on board {CtrlNbr}.");
            position.UpdateOrder(newOrder);
        }

        var changes = _positions
            .Where(p => beforeState.TryGetValue(p.CtrlNbr.Value, out var prev) && prev.PreviousOrder != p.PositionOrder)
            .Select(p => new
            {
                PositionCtrlNbr = p.CtrlNbr.Value,
                EmployeeCtrlNbr = beforeState[p.CtrlNbr.Value].EmployeeCtrlNbr.Value,
                PreviousOrder = beforeState[p.CtrlNbr.Value].PreviousOrder,
                NewOrder = p.PositionOrder
            })
            .ToList();

        if (changes.Count > 0)
        {
            Raise(new PositionsReorderedDomainEvent(CtrlNbr, changes));
        }
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

    internal void UpdateOrder(int newOrder)
    {
        PositionOrder = newOrder;
    }
}
