using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class ShiftDefinition : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public ControlNumber? DepartmentCtrlNbr { get; private set; }
    public string ShiftCode { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public TimeOnly DefaultStartTime { get; private set; }
    public TimeOnly DefaultEndTime { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    private ShiftDefinition()
    {
        WorkAreaGroupCtrlNbr = null!;
    }

    public static ShiftDefinition Create(
        ControlNumber workAreaGroupCtrlNbr,
        string shiftCode,
        string displayName,
        TimeOnly defaultStartTime,
        TimeOnly defaultEndTime,
        int displayOrder,
        bool isActive,
        ControlNumber? departmentCtrlNbr = null)
    {
        return new ShiftDefinition
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            DepartmentCtrlNbr = departmentCtrlNbr,
            ShiftCode = shiftCode,
            DisplayName = displayName,
            DefaultStartTime = defaultStartTime,
            DefaultEndTime = defaultEndTime,
            DisplayOrder = displayOrder,
            IsActive = isActive
        };
    }

    public void Update(
        string? shiftCode = null,
        string? displayName = null,
        TimeOnly? defaultStartTime = null,
        TimeOnly? defaultEndTime = null,
        int? displayOrder = null,
        bool? isActive = null,
        ControlNumber? departmentCtrlNbr = null)
    {
        if (shiftCode is not null) ShiftCode = shiftCode;
        if (displayName is not null) DisplayName = displayName;
        if (defaultStartTime.HasValue) DefaultStartTime = defaultStartTime.Value;
        if (defaultEndTime.HasValue) DefaultEndTime = defaultEndTime.Value;
        if (displayOrder.HasValue) DisplayOrder = displayOrder.Value;
        if (isActive.HasValue) IsActive = isActive.Value;
        DepartmentCtrlNbr = departmentCtrlNbr;
    }
}
