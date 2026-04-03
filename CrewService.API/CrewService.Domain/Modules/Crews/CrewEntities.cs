using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Crews;

public sealed class Crew : Entity
{
    public string CrewType { get; private set; } = "REGULAR";
    public ControlNumber HomeGroupCtrlNbr { get; private set; }
    public ControlNumber? DepartmentCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private Crew() { HomeGroupCtrlNbr = null!; }

    public static Crew Create(string crewType, ControlNumber homeGroupCtrlNbr, string name, bool isActive = true, ControlNumber? departmentCtrlNbr = null)
    {
        var crew = new Crew
        {
            CrewType = crewType,
            HomeGroupCtrlNbr = homeGroupCtrlNbr,
            DepartmentCtrlNbr = departmentCtrlNbr,
            Name = name,
            IsActive = isActive
        };
        crew.Raise(new CrewCreatedDomainEvent(crew));
        return crew;
    }

    public void Update(string name, bool isActive, ControlNumber? departmentCtrlNbr = null)
    {
        Name = name;
        IsActive = isActive;
        DepartmentCtrlNbr = departmentCtrlNbr;
        Raise(new CrewUpdatedDomainEvent(this));
    }
}

public sealed class CrewPosition : Entity
{
    public ControlNumber CrewCtrlNbr { get; private set; }
    public ControlNumber CraftRoleCtrlNbr { get; private set; }
    public int DisplayOrder { get; private set; }

    private CrewPosition() { CrewCtrlNbr = null!; CraftRoleCtrlNbr = null!; }

    public static CrewPosition Create(ControlNumber crewCtrlNbr, ControlNumber craftRoleCtrlNbr, int displayOrder)
    {
        return new CrewPosition
        {
            CrewCtrlNbr = crewCtrlNbr,
            CraftRoleCtrlNbr = craftRoleCtrlNbr,
            DisplayOrder = displayOrder
        };
    }
}

public sealed class CrewIncumbency : Entity
{
    public ControlNumber CrewPositionCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }

    private CrewIncumbency() { CrewPositionCtrlNbr = null!; EmployeeCtrlNbr = null!; }

    public static CrewIncumbency Create(ControlNumber crewPositionCtrlNbr, ControlNumber employeeCtrlNbr, DateTime startUtc, DateTime? endUtc = null)
    {
        return new CrewIncumbency
        {
            CrewPositionCtrlNbr = crewPositionCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            StartUtc = startUtc,
            EndUtc = endUtc
        };
    }
}

public sealed class CrewAttachmentInstance : Entity
{
    public ControlNumber WorkInstanceCtrlNbr { get; private set; }
    public ControlNumber CrewCtrlNbr { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }

    private CrewAttachmentInstance() { WorkInstanceCtrlNbr = null!; CrewCtrlNbr = null!; }

    public static CrewAttachmentInstance Create(ControlNumber workInstanceCtrlNbr, ControlNumber crewCtrlNbr, DateTime startUtc, DateTime? endUtc = null)
    {
        return new CrewAttachmentInstance
        {
            WorkInstanceCtrlNbr = workInstanceCtrlNbr,
            CrewCtrlNbr = crewCtrlNbr,
            StartUtc = startUtc,
            EndUtc = endUtc
        };
    }
}

public sealed class CrewAssignment : Entity
{
    public ControlNumber CrewCtrlNbr { get; private set; }
    public ControlNumber AssignmentCtrlNbr { get; private set; }
    public int DaysOfWeekMask { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }

    private CrewAssignment() { CrewCtrlNbr = null!; AssignmentCtrlNbr = null!; }

    public static CrewAssignment Create(ControlNumber crewCtrlNbr, ControlNumber assignmentCtrlNbr,
        int daysOfWeekMask, DateTime startUtc, DateTime? endUtc = null)
    {
        return new CrewAssignment
        {
            CrewCtrlNbr = crewCtrlNbr,
            AssignmentCtrlNbr = assignmentCtrlNbr,
            DaysOfWeekMask = daysOfWeekMask,
            StartUtc = startUtc,
            EndUtc = endUtc
        };
    }

    public void Update(int daysOfWeekMask, DateTime startUtc, DateTime? endUtc)
    {
        DaysOfWeekMask = daysOfWeekMask;
        StartUtc = startUtc;
        EndUtc = endUtc;
    }
}

// Domain Events
public sealed record CrewCreatedDomainEvent : DomainEvent
{
    public CrewCreatedDomainEvent(Crew c)
        : base(nameof(Crew), c.CtrlNbr.Value, new { c.CrewType, c.Name }) { }
}

public sealed record CrewUpdatedDomainEvent : DomainEvent
{
    public CrewUpdatedDomainEvent(Crew c)
        : base(nameof(Crew), c.CtrlNbr.Value, new { c.Name, c.IsActive }) { }
}

public sealed record CrewPositionVacatedDomainEvent : DomainEvent
{
    public CrewPositionVacatedDomainEvent(CrewPosition p, ControlNumber? previousIncumbentCtrlNbr, string vacancyReasonCode)
        : base(nameof(CrewPosition), p.CtrlNbr.Value, new { CrewCtrlNbr = p.CrewCtrlNbr.Value, CraftRoleCtrlNbr = p.CraftRoleCtrlNbr.Value, PreviousIncumbentCtrlNbr = previousIncumbentCtrlNbr, VacancyReasonCode = vacancyReasonCode }) { }
}
