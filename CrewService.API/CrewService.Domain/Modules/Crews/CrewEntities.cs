using CrewService.Domain.DomainEvents;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Crews;

public sealed class Crew : Entity
{
    public string CrewType { get; private set; } = "REGULAR";
    public ControlNumber WorkAreaCtrlNbr { get; private set; }
    public ControlNumber? DepartmentCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime? AbolishedDate { get; private set; }

    private Crew() { WorkAreaCtrlNbr = null!; }

    public static Crew Create(string crewType, ControlNumber workAreaCtrlNbr, string name, bool isActive = true, ControlNumber? departmentCtrlNbr = null, DateTime? effectiveDate = null, DateTime? abolishedDate = null)
    {
        var crew = new Crew
        {
            CrewType = crewType,
            WorkAreaCtrlNbr = workAreaCtrlNbr,
            DepartmentCtrlNbr = departmentCtrlNbr,
            Name = name,
            IsActive = isActive,
            EffectiveDate = effectiveDate ?? DateTime.UtcNow,
            AbolishedDate = abolishedDate
        };
        crew.Raise(new CrewCreatedDomainEvent(crew));
        return crew;
    }

    public void Update(string name, bool isActive, ControlNumber? departmentCtrlNbr = null, DateTime? effectiveDate = null, DateTime? abolishedDate = null, string? crewType = null)
    {
        Name = name;
        IsActive = isActive;
        DepartmentCtrlNbr = departmentCtrlNbr;
        if (effectiveDate.HasValue) EffectiveDate = effectiveDate.Value;
        AbolishedDate = abolishedDate;
        if (!string.IsNullOrWhiteSpace(crewType)) CrewType = crewType;
        Raise(new CrewUpdatedDomainEvent(this));
    }
}

public sealed class CrewPosition : Entity
{
    public ControlNumber CrewCtrlNbr { get; private set; }
    public ControlNumber CraftRoleCtrlNbr { get; private set; }
    public ControlNumber StaffablePositionCtrlNbr { get; private set; }
    public int DisplayOrder { get; private set; }

    private CrewPosition() { CrewCtrlNbr = null!; CraftRoleCtrlNbr = null!; StaffablePositionCtrlNbr = null!; }

    public static CrewPosition Create(ControlNumber crewCtrlNbr, ControlNumber craftRoleCtrlNbr, int displayOrder,
        ControlNumber staffablePositionCtrlNbr)
    {
        var position = new CrewPosition
        {
            CrewCtrlNbr = crewCtrlNbr,
            CraftRoleCtrlNbr = craftRoleCtrlNbr,
            StaffablePositionCtrlNbr = staffablePositionCtrlNbr,
            DisplayOrder = displayOrder
        };
        position.Raise(new CrewPositionCreatedDomainEvent(position));
        return position;
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
        var incumbency = new CrewIncumbency
        {
            CrewPositionCtrlNbr = crewPositionCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            StartUtc = startUtc,
            EndUtc = endUtc
        };
        incumbency.Raise(new CrewIncumbencyCreatedDomainEvent(incumbency));
        return incumbency;
    }

    public void End(DateTime endUtc)
    {
        EndUtc = endUtc;
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
        var attachment = new CrewAttachmentInstance
        {
            WorkInstanceCtrlNbr = workInstanceCtrlNbr,
            CrewCtrlNbr = crewCtrlNbr,
            StartUtc = startUtc,
            EndUtc = endUtc
        };
        attachment.Raise(new CrewAttachmentInstanceCreatedDomainEvent(attachment));
        return attachment;
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
        var ca = new CrewAssignment
        {
            CrewCtrlNbr = crewCtrlNbr,
            AssignmentCtrlNbr = assignmentCtrlNbr,
            DaysOfWeekMask = daysOfWeekMask,
            StartUtc = startUtc,
            EndUtc = endUtc
        };
        ca.Raise(new CrewAssignmentCreatedDomainEvent(ca));
        return ca;
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

public sealed record CrewPositionCreatedDomainEvent : DomainEvent
{
    public CrewPositionCreatedDomainEvent(CrewPosition p)
        : base(nameof(CrewPosition), p.CtrlNbr.Value, new { CrewCtrlNbr = p.CrewCtrlNbr.Value, CraftRoleCtrlNbr = p.CraftRoleCtrlNbr.Value, p.DisplayOrder }) { }
}

public sealed record CrewPositionVacatedDomainEvent : DomainEvent
{
    public CrewPositionVacatedDomainEvent(CrewPosition p, ControlNumber? previousIncumbentCtrlNbr, string vacancyReasonCode)
        : base(nameof(CrewPosition), p.CtrlNbr.Value, new { CrewCtrlNbr = p.CrewCtrlNbr.Value, CraftRoleCtrlNbr = p.CraftRoleCtrlNbr.Value, PreviousIncumbentCtrlNbr = previousIncumbentCtrlNbr, VacancyReasonCode = vacancyReasonCode }) { }
}

public sealed record CrewIncumbencyCreatedDomainEvent : DomainEvent
{
    public CrewIncumbencyCreatedDomainEvent(CrewIncumbency i)
        : base(nameof(CrewIncumbency), i.CtrlNbr.Value, new { CrewPositionCtrlNbr = i.CrewPositionCtrlNbr.Value, EmployeeCtrlNbr = i.EmployeeCtrlNbr.Value }) { }
}

public sealed record CrewAttachmentInstanceCreatedDomainEvent : DomainEvent
{
    public CrewAttachmentInstanceCreatedDomainEvent(CrewAttachmentInstance a)
        : base(nameof(CrewAttachmentInstance), a.CtrlNbr.Value, new { WorkInstanceCtrlNbr = a.WorkInstanceCtrlNbr.Value, CrewCtrlNbr = a.CrewCtrlNbr.Value }) { }
}

public sealed record CrewAssignmentCreatedDomainEvent : DomainEvent
{
    public CrewAssignmentCreatedDomainEvent(CrewAssignment ca)
        : base(nameof(CrewAssignment), ca.CtrlNbr.Value, new { CrewCtrlNbr = ca.CrewCtrlNbr.Value, AssignmentCtrlNbr = ca.AssignmentCtrlNbr.Value, ca.DaysOfWeekMask }) { }
}
