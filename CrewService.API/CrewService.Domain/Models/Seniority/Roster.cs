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
        return new Roster(
            ControlNumber.Create(craftCtrlNbr),
            ControlNumber.Create(railroadPayrollDepartmentCtrlNbr),
            rosterName,
            rosterPluralName,
            rosterNumber,
            training,
            extraBoard,
            overtimeBoard);
    }

    public Roster Update(
        string? rosterName = null,
        string? rosterPluralName = null,
        int? rosterNumber = null,
        bool? training = null,
        bool? extraBoard = null,
        bool? overtimeBoard = null)
    {
        if (rosterName is not null) RosterName = rosterName;
        if (rosterPluralName is not null) RosterPluralName = rosterPluralName;
        if (rosterNumber is not null) RosterNumber = rosterNumber.Value;
        if (training is not null) Training = training.Value;
        if (extraBoard is not null) ExtraBoard = extraBoard.Value;
        if (overtimeBoard is not null) OvertimeBoard = overtimeBoard.Value;

        return this;
    }
}