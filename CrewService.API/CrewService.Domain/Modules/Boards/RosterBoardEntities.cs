using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Boards;

public sealed class RosterBoard : Entity
{
    private readonly List<RosterBoardPosition> _positions = [];

    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public ControlNumber CraftCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public IReadOnlyList<RosterBoardPosition> Positions => _positions.AsReadOnly();

    private RosterBoard()
    {
        WorkAreaGroupCtrlNbr = null!;
        CraftCtrlNbr = null!;
    }

    public static RosterBoard Create(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr,
        string name, bool isActive = true)
    {
        return new RosterBoard
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            CraftCtrlNbr = craftCtrlNbr,
            Name = name,
            IsActive = isActive,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public RosterBoardPosition AddPosition(ControlNumber employeeCtrlNbr, int positionOrder)
    {
        var position = RosterBoardPosition.Create(CtrlNbr, employeeCtrlNbr, positionOrder);
        _positions.Add(position);
        return position;
    }

    public void Deactivate()
    {
        IsActive = false;
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }
}

public sealed class RosterBoardPosition : Entity
{
    public ControlNumber RosterBoardCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public int PositionOrder { get; private set; }
    public string HangoutStatus { get; private set; } = "Active";
    public DateTime? HangoutAtUtc { get; private set; }

    private RosterBoardPosition()
    {
        RosterBoardCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    internal static RosterBoardPosition Create(
        ControlNumber rosterBoardCtrlNbr, ControlNumber employeeCtrlNbr, int positionOrder)
    {
        return new RosterBoardPosition
        {
            RosterBoardCtrlNbr = rosterBoardCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            PositionOrder = positionOrder,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public void Hangout()
    {
        HangoutStatus = "HungOut";
        HangoutAtUtc = DateTime.UtcNow;
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void MarkOff()
    {
        HangoutStatus = "MarkedOff";
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public void RestoreFromHangout()
    {
        HangoutStatus = "Active";
        HangoutAtUtc = null;
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }
}
