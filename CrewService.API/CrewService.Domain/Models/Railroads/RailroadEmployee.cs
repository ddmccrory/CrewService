using CrewService.Domain.DomainEvents.Railroads;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.Railroads;

public sealed class RailroadEmployee : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public ControlNumber RailroadCtrlNbr { get; private set; }
    public bool AssignedPoolsOnly { get; private set; }

    private RailroadEmployee()
    {
        EmployeeCtrlNbr = ControlNumber.Create();
        RailroadCtrlNbr = ControlNumber.Create();
    }

    private RailroadEmployee(ControlNumber employeeCtrlNbr, ControlNumber railroadCtrlNbr, bool assignedPoolsOnly)
    {
        EmployeeCtrlNbr = employeeCtrlNbr;
        RailroadCtrlNbr = railroadCtrlNbr;
        AssignedPoolsOnly = assignedPoolsOnly;
    }

    public static RailroadEmployee Create(long employeeCtrlNbr, long railroadCtrlNbr, bool assignedPoolsOnly = false)
    {
        var entity = new RailroadEmployee(ControlNumber.Create(employeeCtrlNbr), ControlNumber.Create(railroadCtrlNbr), assignedPoolsOnly);
        entity.Raise(new RailroadEmployeeCreatedDomainEvent(entity.CtrlNbr));
        return entity;
    }

    public RailroadEmployee Update(bool? assignedPoolsOnly = null)
    {
        var changes = new Dictionary<string, object?>();

        if (assignedPoolsOnly.HasValue)
        {
            AssignedPoolsOnly = assignedPoolsOnly.Value;
            changes["assignedPoolsOnly"] = assignedPoolsOnly.Value;
        }

        if (changes.Count > 0)
        {
            Raise(new RailroadEmployeeUpdatedDomainEvent(CtrlNbr, payload: new { Changes = changes }));
        }

        return this;
    }

    public void Delete()
    {
        Raise(new RailroadEmployeeDeletedDomainEvent(CtrlNbr, payload: new { DeletedAt = DateTime.UtcNow }));
    }
}
