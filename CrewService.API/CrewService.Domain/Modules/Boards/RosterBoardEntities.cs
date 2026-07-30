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
    /// <summary>Whether employees on this board are permitted to bid on open bulletins. Defaults based on board type.</summary>
    public bool AllowBulletinBidding { get; private set; }
    /// <summary>Whether a seniority move can target positions on this board. Defaults based on board type.</summary>
    public bool AllowSeniorityMove { get; private set; }
    /// <summary>Whether employees on this board are eligible to be force-assigned to no-bid crew
    /// vacancies. Mirrors SA's per-board <c>RosterBoard.ForceAssign</c> flag and keeps the
    /// force-assign candidate pool tenant-configurable rather than hardcoding board types in the
    /// selection logic. Defaults based on board type.</summary>
    public bool AllowForceAssign { get; private set; }

    /// <summary>Whether landing on this board raises an <c>EmployeeNotification</c> to the affected
    /// employee, regardless of how they were placed (manual add, seniority move, or seniority-state
    /// change). Emulates SA's hangout-board notification while keeping the trigger tenant-configurable
    /// per board rather than hardcoding board types. Defaults based on board type.</summary>
    public bool NotifyOnPlacement { get; private set; }

    /// <summary>Whether the placement notification requires the employee's acknowledgement. Only
    /// meaningful when <see cref="NotifyOnPlacement"/> is enabled. Mirrors SA's required-acknowledgement
    /// behavior for hangout placement. Defaults based on board type.</summary>
    public bool PlacementRequiresAcknowledgement { get; private set; }

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
            RequiredPositions = requiredPositions,
            AllowBulletinBidding = DefaultAllowBulletinBidding(boardType),
            AllowSeniorityMove = DefaultAllowSeniorityMove(boardType),
            AllowForceAssign = DefaultAllowForceAssign(boardType),
            NotifyOnPlacement = DefaultNotifyOnPlacement(boardType),
            PlacementRequiresAcknowledgement = DefaultPlacementRequiresAcknowledgement(boardType)
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

    public void SetAllowBulletinBidding(bool value) => AllowBulletinBidding = value;

    public void SetAllowSeniorityMove(bool value) => AllowSeniorityMove = value;

    public void SetAllowForceAssign(bool value) => AllowForceAssign = value;

    public void SetNotifyOnPlacement(bool value)
    {
        NotifyOnPlacement = value;
        if (!value)
            PlacementRequiresAcknowledgement = false;
    }

    public void SetPlacementRequiresAcknowledgement(bool value) =>
        PlacementRequiresAcknowledgement = NotifyOnPlacement && value;

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
    /// <summary>Returns the conventional default for AllowBulletinBidding based on board type.
    /// ExtraBoard and Hangout boards allow bidding; all others do not.</summary>
    public static bool DefaultAllowBulletinBidding(BoardType boardType) =>
        boardType is BoardType.ExtraBoard or BoardType.Hangout;

    /// <summary>Returns the conventional default for AllowSeniorityMove based on board type.
    /// Only ExtraBoard allows seniority moves by default.</summary>
    public static bool DefaultAllowSeniorityMove(BoardType boardType) =>
        boardType is BoardType.ExtraBoard;

    /// <summary>Returns the conventional default for AllowForceAssign based on board type.
    /// ExtraBoard and Hangout members form the legacy non-crew force-assign pool, so both are
    /// eligible by default; all other board types are not.</summary>
    public static bool DefaultAllowForceAssign(BoardType boardType) =>
        boardType is BoardType.ExtraBoard or BoardType.Hangout;

    /// <summary>Returns the conventional default for NotifyOnPlacement based on board type.
    /// Emulating SA, only Hangout placement notifies the employee by default; every other board
    /// type is silent unless a railroad opts in.</summary>
    public static bool DefaultNotifyOnPlacement(BoardType boardType) =>
        boardType is BoardType.Hangout;

    /// <summary>Returns the conventional default for PlacementRequiresAcknowledgement based on board
    /// type. Emulating SA, Hangout placement requires acknowledgement by default; other board types
    /// do not (and it only applies when <see cref="NotifyOnPlacement"/> is enabled).</summary>
    public static bool DefaultPlacementRequiresAcknowledgement(BoardType boardType) =>
        boardType is BoardType.Hangout;
}

public sealed class RosterBoardPosition : Entity
{
    public ControlNumber RosterBoardCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber StaffablePositionCtrlNbr { get; private set; }
    public int PositionOrder { get; private set; }

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

    internal void UpdateOrder(int newOrder)
    {
        PositionOrder = newOrder;
    }
}
