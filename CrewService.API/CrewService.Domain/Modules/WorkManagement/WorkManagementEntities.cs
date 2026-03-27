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

    public static AssignmentTemplate Create(ControlNumber workAreaGroupCtrlNbr, string code, string name, string? recurrenceJson, bool isActive = true)
    {
        var template = new AssignmentTemplate(workAreaGroupCtrlNbr, code, name, recurrenceJson, isActive);
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

    public static WorkInstance Create(ControlNumber? assignmentTemplateCtrlNbr, ControlNumber workAreaGroupCtrlNbr,
        DateTime startUtc, DateTime endUtc, DateTime? callTimeUtc, string status = "Planned")
    {
        var instance = new WorkInstance(
            assignmentTemplateCtrlNbr,
            workAreaGroupCtrlNbr,
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


public sealed class Department : Entity
{
    public long ParentCtrlNbr { get; private set; }
    public ControlNumber? DynamicGroupCtrlNbr { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private Department() { }

    public static Department Create(long parentCtrlNbr, ControlNumber? dynamicGroupCtrlNbr, string name)
    {
        return new Department
        {
            ParentCtrlNbr = parentCtrlNbr,
            DynamicGroupCtrlNbr = dynamicGroupCtrlNbr,
            Name = name
        };
    }

    public void Update(string name)
    {
        Name = name;
    }
}
public sealed class PositionRole : Entity
{
    public ControlNumber CraftCtrlNbr { get; private set; }
    public string? Code { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? AlternateName { get; private set; }

    private PositionRole() { CraftCtrlNbr = null!; }

    public static PositionRole Create(ControlNumber craftCtrlNbr, string? code, string name, string? alternateName = null)
    {
        return new PositionRole
        {
            CraftCtrlNbr = craftCtrlNbr,
            Code = code,
            Name = name,
            AlternateName = alternateName
        };
    }

    public void Update(string? code, string name, string? alternateName)
    {
        Code = code;
        Name = name;
        AlternateName = alternateName;
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

    public static PositionSlot Create(ControlNumber workInstanceCtrlNbr, ControlNumber positionRoleCtrlNbr)
    {
        return new PositionSlot
        {
            WorkInstanceCtrlNbr = workInstanceCtrlNbr,
            PositionRoleCtrlNbr = positionRoleCtrlNbr,
            Status = "Created"
        };
    }

    public void Bind(ControlNumber employeeCtrlNbr, string source)
    {
        BoundEmployeeCtrlNbr = employeeCtrlNbr;
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

    public static SlotRequirement Create(ControlNumber positionSlotCtrlNbr, int priority,
        ControlNumber? positionRoleCtrlNbr = null, ControlNumber? qualificationTypeCtrlNbr = null, string? notes = null)
    {
        return new SlotRequirement
        {
            PositionSlotCtrlNbr = positionSlotCtrlNbr,
            Priority = priority,
            PositionRoleCtrlNbr = positionRoleCtrlNbr,
            QualificationTypeCtrlNbr = qualificationTypeCtrlNbr,
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
