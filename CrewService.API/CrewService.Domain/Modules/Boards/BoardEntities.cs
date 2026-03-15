using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Boards;

public sealed class ExtraBoard : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }
    public ControlNumber PlacedGroupCtrlNbr { get; private set; }
    public string BoardKind { get; private set; } = "PRIMARY";
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string? AuxBoardType { get; private set; }

    private ExtraBoard() { CraftCtrlNbr = null!; PlacedGroupCtrlNbr = null!; }

    public static ExtraBoard Create(ControlNumber craftCtrlNbr, ControlNumber placedGroupCtrlNbr, string boardKind, string name, bool isActive = true, string? auxBoardType = null)
    {
        var board = new ExtraBoard
        {
            CraftCtrlNbr = craftCtrlNbr,
            PlacedGroupCtrlNbr = placedGroupCtrlNbr,
            BoardKind = boardKind,
            Name = name,
            IsActive = isActive,
            AuxBoardType = auxBoardType
        };
        board.Raise(new ExtraBoardCreatedDomainEvent(board));
        return board;
    }

    public void Update(string name, bool isActive, string? auxBoardType)
    {
        Name = name;
        IsActive = isActive;
        AuxBoardType = auxBoardType;
        Raise(new ExtraBoardUpdatedDomainEvent(this));
    }
}

public sealed class BoardMember : Entity
{
    public ControlNumber ExtraBoardCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public int OrderIndex { get; private set; }
    public string? StateJson { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }

    private BoardMember() { ExtraBoardCtrlNbr = null!; EmployeeCtrlNbr = null!; }

    public static BoardMember Create(ControlNumber extraBoardCtrlNbr, ControlNumber employeeCtrlNbr, int orderIndex, DateTime startUtc, DateTime? endUtc = null, string? stateJson = null)
    {
        return new BoardMember
        {
            ExtraBoardCtrlNbr = extraBoardCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            OrderIndex = orderIndex,
            StateJson = stateJson,
            StartUtc = startUtc,
            EndUtc = endUtc
        };
    }

    public void AdvanceState(int newOrderIndex, string? stateJson)
    {
        OrderIndex = newOrderIndex;
        StateJson = stateJson;
    }
}

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

// Domain Events
public sealed record ExtraBoardCreatedDomainEvent : DomainEvent
{
    public ExtraBoardCreatedDomainEvent(ExtraBoard b) : base(nameof(ExtraBoard), b.CtrlNbr.Value, new { b.BoardKind, b.Name }) { }
}

public sealed record ExtraBoardUpdatedDomainEvent : DomainEvent
{
    public ExtraBoardUpdatedDomainEvent(ExtraBoard b) : base(nameof(ExtraBoard), b.CtrlNbr.Value, new { b.Name, b.IsActive }) { }
}

public sealed record ExtraBoardVacatedDomainEvent : DomainEvent
{
    public ExtraBoardVacatedDomainEvent(ExtraBoard b, ControlNumber? previousMemberCtrlNbr, string vacancyReasonCode)
        : base(nameof(ExtraBoard), b.CtrlNbr.Value, new { CraftCtrlNbr = b.CraftCtrlNbr.Value, PreviousMemberCtrlNbr = previousMemberCtrlNbr, VacancyReasonCode = vacancyReasonCode }) { }
}
