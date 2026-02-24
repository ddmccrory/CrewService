using CrewService.Domain.DomainEvents;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class AssignmentTemplate : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? RecurrenceJson { get; private set; }
    public bool IsActive { get; private set; }

    private AssignmentTemplate() { WorkAreaGroupCtrlNbr = null!; }

    private AssignmentTemplate(ControlNumber workAreaGroupCtrlNbr, string code, string name, string? recurrenceJson, bool isActive)
    {
        WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr;
        Code = code;
        Name = name;
        RecurrenceJson = recurrenceJson;
        IsActive = isActive;
    }

    public static AssignmentTemplate Create(long workAreaGroupCtrlNbr, string code, string name, string? recurrenceJson, bool isActive = true)
    {
        var template = new AssignmentTemplate(ControlNumber.Create(workAreaGroupCtrlNbr), code, name, recurrenceJson, isActive);
        template.Raise(new AssignmentTemplateCreatedDomainEvent(template));
        return template;
    }

    public void Update(string code, string name, string? recurrenceJson, bool isActive)
    {
        Code = code;
        Name = name;
        RecurrenceJson = recurrenceJson;
        IsActive = isActive;
        Raise(new AssignmentTemplateUpdatedDomainEvent(this));
    }
}

public sealed class WorkInstance : Entity
{
    public ControlNumber? AssignmentTemplateCtrlNbr { get; private set; }
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public DateTime? CallTimeUtc { get; private set; }
    public string Status { get; private set; } = "Planned";

    private WorkInstance() { WorkAreaGroupCtrlNbr = null!; }

    private WorkInstance(ControlNumber? assignmentTemplateCtrlNbr, ControlNumber workAreaGroupCtrlNbr,
        DateTime startUtc, DateTime endUtc, DateTime? callTimeUtc, string status)
    {
        AssignmentTemplateCtrlNbr = assignmentTemplateCtrlNbr;
        WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr;
        StartUtc = startUtc;
        EndUtc = endUtc;
        CallTimeUtc = callTimeUtc;
        Status = status;
    }

    public static WorkInstance Create(long? assignmentTemplateCtrlNbr, long workAreaGroupCtrlNbr,
        DateTime startUtc, DateTime endUtc, DateTime? callTimeUtc, string status = "Planned")
    {
        var instance = new WorkInstance(
            assignmentTemplateCtrlNbr.HasValue ? ControlNumber.Create(assignmentTemplateCtrlNbr.Value) : null,
            ControlNumber.Create(workAreaGroupCtrlNbr),
            startUtc, endUtc, callTimeUtc, status);
        instance.Raise(new WorkInstanceCreatedDomainEvent(instance));
        return instance;
    }

    public void UpdateStatus(string status)
    {
        Status = status;
        Raise(new WorkInstanceUpdatedDomainEvent(this));
    }
}

public sealed class PositionRole : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private PositionRole() { CraftCtrlNbr = null!; }

    public static PositionRole Create(long craftCtrlNbr, string code, string name)
    {
        return new PositionRole
        {
            CraftCtrlNbr = ControlNumber.Create(craftCtrlNbr),
            Code = code,
            Name = name
        };
    }
}

public sealed class PositionSlot : Entity
{
    public ControlNumber WorkInstanceCtrlNbr { get; private set; }
    public ControlNumber PositionRoleCtrlNbr { get; private set; }
    public string Status { get; private set; } = "Created";
    public ControlNumber? BoundEmployeeCtrlNbr { get; private set; }
    public string? BindingSource { get; private set; }

    private PositionSlot() { WorkInstanceCtrlNbr = null!; PositionRoleCtrlNbr = null!; }

    public static PositionSlot Create(long workInstanceCtrlNbr, long positionRoleCtrlNbr)
    {
        return new PositionSlot
        {
            WorkInstanceCtrlNbr = ControlNumber.Create(workInstanceCtrlNbr),
            PositionRoleCtrlNbr = ControlNumber.Create(positionRoleCtrlNbr),
            Status = "Created"
        };
    }

    public void Bind(long employeeCtrlNbr, string source)
    {
        BoundEmployeeCtrlNbr = ControlNumber.Create(employeeCtrlNbr);
        BindingSource = source;
        Status = "Filled";
        Raise(new PositionSlotBoundDomainEvent(this));
    }

    public void Unbind()
    {
        BoundEmployeeCtrlNbr = null;
        BindingSource = null;
        Status = "Open";
        Raise(new PositionSlotUnboundDomainEvent(this));
    }
}

public sealed class SlotRequirement : Entity
{
    public ControlNumber PositionSlotCtrlNbr { get; private set; }
    public int Priority { get; private set; }
    public ControlNumber? PositionRoleCtrlNbr { get; private set; }
    public ControlNumber? QualificationTypeCtrlNbr { get; private set; }
    public string? Notes { get; private set; }

    private SlotRequirement() { PositionSlotCtrlNbr = null!; }

    public static SlotRequirement Create(long positionSlotCtrlNbr, int priority,
        long? positionRoleCtrlNbr = null, long? qualificationTypeCtrlNbr = null, string? notes = null)
    {
        return new SlotRequirement
        {
            PositionSlotCtrlNbr = ControlNumber.Create(positionSlotCtrlNbr),
            Priority = priority,
            PositionRoleCtrlNbr = positionRoleCtrlNbr.HasValue ? ControlNumber.Create(positionRoleCtrlNbr.Value) : null,
            QualificationTypeCtrlNbr = qualificationTypeCtrlNbr.HasValue ? ControlNumber.Create(qualificationTypeCtrlNbr.Value) : null,
            Notes = notes
        };
    }
}

// Domain Events
public sealed record AssignmentTemplateCreatedDomainEvent : DomainEvent
{
    public AssignmentTemplateCreatedDomainEvent(AssignmentTemplate t)
        : base(nameof(AssignmentTemplate), t.CtrlNbr.Value, new { t.Code, t.Name, WorkAreaGroupCtrlNbr = t.WorkAreaGroupCtrlNbr.Value }) { }
}

public sealed record AssignmentTemplateUpdatedDomainEvent : DomainEvent
{
    public AssignmentTemplateUpdatedDomainEvent(AssignmentTemplate t)
        : base(nameof(AssignmentTemplate), t.CtrlNbr.Value, new { t.Code, t.Name, t.IsActive }) { }
}

public sealed record WorkInstanceCreatedDomainEvent : DomainEvent
{
    public WorkInstanceCreatedDomainEvent(WorkInstance w)
        : base(nameof(WorkInstance), w.CtrlNbr.Value, new { w.Status, WorkAreaGroupCtrlNbr = w.WorkAreaGroupCtrlNbr.Value }) { }
}

public sealed record WorkInstanceUpdatedDomainEvent : DomainEvent
{
    public WorkInstanceUpdatedDomainEvent(WorkInstance w)
        : base(nameof(WorkInstance), w.CtrlNbr.Value, new { w.Status }) { }
}

public sealed record PositionSlotBoundDomainEvent : DomainEvent
{
    public PositionSlotBoundDomainEvent(PositionSlot s)
        : base(nameof(PositionSlot), s.CtrlNbr.Value, new { BoundEmployeeCtrlNbr = s.BoundEmployeeCtrlNbr?.Value, s.BindingSource }) { }
}

public sealed record PositionSlotUnboundDomainEvent : DomainEvent
{
    public PositionSlotUnboundDomainEvent(PositionSlot s)
        : base(nameof(PositionSlot), s.CtrlNbr.Value) { }
}
