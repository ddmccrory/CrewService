using CrewService.Domain.DomainEvents.Seniority;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Seniority;

public sealed class Roster : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public ControlNumber? RailroadPayrollDepartmentCtrlNbr { get; private set; }
    public string RosterName { get; private set; } = string.Empty;
    public string RosterPluralName { get; private set; } = string.Empty;
    public int RosterNumber { get; private set; }

    private Roster()
    {
        CraftCtrlNbr = null!;
        WorkAreaGroupCtrlNbr = null!;
    }

    private Roster(
        ControlNumber craftCtrlNbr,
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber? railroadPayrollDepartmentCtrlNbr,
        string rosterName,
        string rosterPluralName,
        int rosterNumber)
    {
        CraftCtrlNbr = craftCtrlNbr;
        WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr;
        RailroadPayrollDepartmentCtrlNbr = railroadPayrollDepartmentCtrlNbr;
        RosterName = rosterName;
        RosterPluralName = rosterPluralName;
        RosterNumber = rosterNumber;
    }

    public static Roster Create(
        ControlNumber craftCtrlNbr,
        ControlNumber workAreaGroupCtrlNbr,
        ControlNumber? railroadPayrollDepartmentCtrlNbr,
        string rosterName,
        string rosterPluralName,
        int rosterNumber)
    {
        var entity = new Roster(
            craftCtrlNbr,
            workAreaGroupCtrlNbr,
            railroadPayrollDepartmentCtrlNbr,
            rosterName,
            rosterPluralName,
            rosterNumber);
        entity.Raise(new RosterCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public Roster Update(
        string? rosterName = null,
        string? rosterPluralName = null,
        int? rosterNumber = null)
    {
        var changes = new Dictionary<string, object?>();

        if (rosterName is not null) { RosterName = rosterName; changes["rosterName"] = rosterName; }
        if (rosterPluralName is not null) { RosterPluralName = rosterPluralName; changes["rosterPluralName"] = rosterPluralName; }
        if (rosterNumber is not null) { RosterNumber = rosterNumber.Value; changes["rosterNumber"] = rosterNumber.Value; }

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