using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Crews;

public sealed class Crew : Entity
{
    public string CrewType { get; private set; } = "REGULAR";
    public ControlNumber HomeGroupCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private Crew() { HomeGroupCtrlNbr = null!; }

    public static Crew Create(string crewType, long homeGroupCtrlNbr, string name, bool isActive = true)
    {
        var crew = new Crew
        {
            CrewType = crewType,
            HomeGroupCtrlNbr = ControlNumber.Create(homeGroupCtrlNbr),
            Name = name,
            IsActive = isActive
        };
        crew.Raise(new CrewCreatedDomainEvent(crew));
        return crew;
    }

    public void Update(string name, bool isActive)
    {
        Name = name;
        IsActive = isActive;
        Raise(new CrewUpdatedDomainEvent(this));
    }
}

public sealed class CrewPosition : Entity
{
    public ControlNumber CrewCtrlNbr { get; private set; }
    public ControlNumber PositionRoleCtrlNbr { get; private set; }
    public int DisplayOrder { get; private set; }

    private CrewPosition() { CrewCtrlNbr = null!; PositionRoleCtrlNbr = null!; }

    public static CrewPosition Create(long crewCtrlNbr, long positionRoleCtrlNbr, int displayOrder)
    {
        return new CrewPosition
        {
            CrewCtrlNbr = ControlNumber.Create(crewCtrlNbr),
            PositionRoleCtrlNbr = ControlNumber.Create(positionRoleCtrlNbr),
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

    public static CrewIncumbency Create(long crewPositionCtrlNbr, long employeeCtrlNbr, DateTime startUtc, DateTime? endUtc = null)
    {
        return new CrewIncumbency
        {
            CrewPositionCtrlNbr = ControlNumber.Create(crewPositionCtrlNbr),
            EmployeeCtrlNbr = ControlNumber.Create(employeeCtrlNbr),
            StartUtc = startUtc,
            EndUtc = endUtc
        };
    }
}

public sealed class CrewAttachmentTemplate : Entity
{
    public ControlNumber AssignmentTemplateCtrlNbr { get; private set; }
    public ControlNumber CrewCtrlNbr { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }

    private CrewAttachmentTemplate() { AssignmentTemplateCtrlNbr = null!; CrewCtrlNbr = null!; }

    public static CrewAttachmentTemplate Create(long assignmentTemplateCtrlNbr, long crewCtrlNbr, DateTime startUtc, DateTime? endUtc = null)
    {
        return new CrewAttachmentTemplate
        {
            AssignmentTemplateCtrlNbr = ControlNumber.Create(assignmentTemplateCtrlNbr),
            CrewCtrlNbr = ControlNumber.Create(crewCtrlNbr),
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

    public static CrewAttachmentInstance Create(long workInstanceCtrlNbr, long crewCtrlNbr, DateTime startUtc, DateTime? endUtc = null)
    {
        return new CrewAttachmentInstance
        {
            WorkInstanceCtrlNbr = ControlNumber.Create(workInstanceCtrlNbr),
            CrewCtrlNbr = ControlNumber.Create(crewCtrlNbr),
            StartUtc = startUtc,
            EndUtc = endUtc
        };
    }
}

public sealed class ReliefCoverageRule : Entity
{
    public ControlNumber ReliefCrewCtrlNbr { get; private set; }
    public ControlNumber AssignmentTemplateCtrlNbr { get; private set; }
    public int DaysOfWeekMask { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }

    private ReliefCoverageRule() { ReliefCrewCtrlNbr = null!; AssignmentTemplateCtrlNbr = null!; }

    public static ReliefCoverageRule Create(long reliefCrewCtrlNbr, long assignmentTemplateCtrlNbr,
        int daysOfWeekMask, DateTime startUtc, DateTime? endUtc = null)
    {
        return new ReliefCoverageRule
        {
            ReliefCrewCtrlNbr = ControlNumber.Create(reliefCrewCtrlNbr),
            AssignmentTemplateCtrlNbr = ControlNumber.Create(assignmentTemplateCtrlNbr),
            DaysOfWeekMask = daysOfWeekMask,
            StartUtc = startUtc,
            EndUtc = endUtc
        };
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
    public CrewPositionVacatedDomainEvent(CrewPosition p, long? previousIncumbentCtrlNbr, string vacancyReasonCode)
        : base(nameof(CrewPosition), p.CtrlNbr.Value, new { CrewCtrlNbr = p.CrewCtrlNbr.Value, PositionRoleCtrlNbr = p.PositionRoleCtrlNbr.Value, PreviousIncumbentCtrlNbr = previousIncumbentCtrlNbr, VacancyReasonCode = vacancyReasonCode }) { }
}
