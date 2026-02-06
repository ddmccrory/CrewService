using CrewService.Domain.DomainEvents.Seniority;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Seniority;

public sealed class Roster : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }
    public ControlNumber RailroadPayrollDepartmentCtrlNbr { get; private set; }
    public string RosterName { get; private set; } = string.Empty;
    public string RosterPluralName { get; private set; } = string.Empty;
    public int RosterNumber { get; private set; }
    public bool Training { get; private set; }
    public bool ExtraBoard { get; private set; }
    public bool OvertimeBoard { get; private set; }

    private Roster()
    {
        CraftCtrlNbr = null!;
        RailroadPayrollDepartmentCtrlNbr = null!;
    }

    private Roster(
        ControlNumber craftCtrlNbr,
        ControlNumber railroadPayrollDepartmentCtrlNbr,
        string rosterName,
        string rosterPluralName,
        int rosterNumber,
        bool training,
        bool extraBoard,
        bool overtimeBoard)
    {
        CraftCtrlNbr = craftCtrlNbr;
        RailroadPayrollDepartmentCtrlNbr = railroadPayrollDepartmentCtrlNbr;
        RosterName = rosterName;
        RosterPluralName = rosterPluralName;
        RosterNumber = rosterNumber;
        Training = training;
        ExtraBoard = extraBoard;
        OvertimeBoard = overtimeBoard;
    }

    public static Roster Create(
        long craftCtrlNbr,
        long railroadPayrollDepartmentCtrlNbr,
        string rosterName,
        string rosterPluralName,
        int rosterNumber,
        bool training,
        bool extraBoard,
        bool overtimeBoard)
    {
        var entity = new Roster(
            ControlNumber.Create(craftCtrlNbr),
            ControlNumber.Create(railroadPayrollDepartmentCtrlNbr),
            rosterName,
            rosterPluralName,
            rosterNumber,
            training,
            extraBoard,
            overtimeBoard);
        entity.Raise(new RosterCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public Roster Update(
        string? rosterName = null,
        string? rosterPluralName = null,
        int? rosterNumber = null,
        bool? training = null,
        bool? extraBoard = null,
        bool? overtimeBoard = null)
    {
        var changes = new Dictionary<string, object?>();

        if (rosterName is not null) { RosterName = rosterName; changes["rosterName"] = rosterName; }
        if (rosterPluralName is not null) { RosterPluralName = rosterPluralName; changes["rosterPluralName"] = rosterPluralName; }
        if (rosterNumber is not null) { RosterNumber = rosterNumber.Value; changes["rosterNumber"] = rosterNumber.Value; }
        if (training is not null) { Training = training.Value; changes["training"] = training.Value; }
        if (extraBoard is not null) { ExtraBoard = extraBoard.Value; changes["extraBoard"] = extraBoard.Value; }
        if (overtimeBoard is not null) { OvertimeBoard = overtimeBoard.Value; changes["overtimeBoard"] = overtimeBoard.Value; }

        if (changes.Count > 0)
        {
            Raise(new RosterUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }

        return this;
    }

    public void Delete()
    {
        Raise(new RosterDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}