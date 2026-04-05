using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Crews;

public sealed class Assignment : Entity
{
    public ControlNumber GroupCtrlNbr { get; private set; }
    public ControlNumber? DepartmentCtrlNbr { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsExtra { get; private set; }
    public bool IsActive { get; private set; }

    private Assignment() { GroupCtrlNbr = null!; }

    public static Assignment Create(
        ControlNumber groupCtrlNbr,
        string code,
        string name,
        bool isExtra = false,
        bool isActive = true,
        ControlNumber? departmentCtrlNbr = null)
    {
        return new Assignment
        {
            GroupCtrlNbr = groupCtrlNbr,
            DepartmentCtrlNbr = departmentCtrlNbr,
            Code = code,
            Name = name,
            IsExtra = isExtra,
            IsActive = isActive
        };
    }

    public void Update(
        string? code = null,
        string? name = null,
        bool? isExtra = null,
        bool? isActive = null,
        ControlNumber? departmentCtrlNbr = null,
        ControlNumber? groupCtrlNbr = null)
    {
        if (code is not null) Code = code;
        if (name is not null) Name = name;
        if (isExtra is not null) IsExtra = isExtra.Value;
        if (isActive is not null) IsActive = isActive.Value;
        DepartmentCtrlNbr = departmentCtrlNbr;
        if (groupCtrlNbr is not null) GroupCtrlNbr = groupCtrlNbr;
    }
}

public sealed class AssignmentSchedule : Entity
{
    public ControlNumber AssignmentCtrlNbr { get; private set; }
    public ControlNumber ShiftDefinitionCtrlNbr { get; private set; }
    public int OperatingDaysMask { get; private set; }
    public TimeOnly OnDutyTime { get; private set; }
    public TimeOnly OffDutyTime { get; private set; }

    private AssignmentSchedule() { AssignmentCtrlNbr = null!; ShiftDefinitionCtrlNbr = null!; }

    public static AssignmentSchedule Create(
        ControlNumber assignmentCtrlNbr,
        ControlNumber shiftDefinitionCtrlNbr,
        int operatingDaysMask,
        TimeOnly onDutyTime,
        TimeOnly offDutyTime)
    {
        return new AssignmentSchedule
        {
            AssignmentCtrlNbr = assignmentCtrlNbr,
            ShiftDefinitionCtrlNbr = shiftDefinitionCtrlNbr,
            OperatingDaysMask = operatingDaysMask,
            OnDutyTime = onDutyTime,
            OffDutyTime = offDutyTime
        };
    }

    public void Update(
        int operatingDaysMask,
        TimeOnly onDutyTime,
        TimeOnly offDutyTime)
    {
        OperatingDaysMask = operatingDaysMask;
        OnDutyTime = onDutyTime;
        OffDutyTime = offDutyTime;
    }
}
