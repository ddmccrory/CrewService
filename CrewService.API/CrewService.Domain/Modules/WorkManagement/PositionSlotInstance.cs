using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class PositionSlotInstance : Entity
{
    public ControlNumber ShiftInstanceCtrlNbr { get; private set; }
    public ControlNumber? CrewPositionCtrlNbr { get; private set; }
    public ControlNumber? IncumbentEmployeeCtrlNbr { get; private set; }
    public ControlNumber AssignmentCtrlNbr { get; private set; }
    public string AssignmentCode { get; private set; } = string.Empty;
    public string AssignmentName { get; private set; } = string.Empty;
    public string CraftRoleName { get; private set; } = string.Empty;
    public string GroupName { get; private set; } = string.Empty;
    public string GroupCode { get; private set; } = string.Empty;
    public TimeOnly OnDutyTime { get; private set; }
    public TimeOnly OffDutyTime { get; private set; }
    public PositionSlotStatus Status { get; private set; } = PositionSlotStatus.Open;
    public bool IsIncumbent { get; private set; }
    public bool IsAnnulled { get; private set; }
    public bool IsDoNotFill { get; private set; }
    public bool IsSkipped { get; private set; }
    public bool IsAdHoc { get; private set; }
    public string? AnnulmentReason { get; private set; }
    public DateTime? AnnulmentDateTimeUtc { get; private set; }
    public int DisplayOrder { get; private set; }
    public string CrewName { get; private set; } = string.Empty;
    public string CrewType { get; private set; } = string.Empty;

    private PositionSlotInstance()
    {
        ShiftInstanceCtrlNbr = null!;
        AssignmentCtrlNbr = null!;
    }

    internal static PositionSlotInstance Create(
        ControlNumber shiftInstanceCtrlNbr,
        ControlNumber crewPositionCtrlNbr,
        ControlNumber? incumbentEmployeeCtrlNbr,
        int displayOrder,
        PositionSlotStatus status,
        bool isIncumbent,
        ControlNumber assignmentCtrlNbr,
        string assignmentCode,
        string assignmentName,
        string craftRoleName,
        string groupName,
        string groupCode,
        TimeOnly onDutyTime,
        TimeOnly offDutyTime,
        string crewName = "",
        string crewType = "")
    {
        return new PositionSlotInstance
        {
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            CrewPositionCtrlNbr = crewPositionCtrlNbr,
            IncumbentEmployeeCtrlNbr = incumbentEmployeeCtrlNbr,
            DisplayOrder = displayOrder,
            Status = status,
            IsIncumbent = isIncumbent,
            AssignmentCtrlNbr = assignmentCtrlNbr,
            AssignmentCode = assignmentCode,
            AssignmentName = assignmentName,
            CraftRoleName = craftRoleName,
            GroupName = groupName,
            GroupCode = groupCode,
            OnDutyTime = onDutyTime,
            OffDutyTime = offDutyTime,
            CrewName = crewName,
            CrewType = crewType
        };
    }

    public void Fill(ControlNumber employeeCtrlNbr)
    {
        Fill(employeeCtrlNbr, isIncumbent: true);
    }

    public void Fill(ControlNumber employeeCtrlNbr, bool isIncumbent)
    {
        IncumbentEmployeeCtrlNbr = employeeCtrlNbr;
        IsIncumbent = isIncumbent;
        Status = PositionSlotStatus.Filled;
    }

    public void SetIncumbent(ControlNumber? employeeCtrlNbr, bool isIncumbent = true)
    {
        IncumbentEmployeeCtrlNbr = employeeCtrlNbr;
        IsIncumbent = employeeCtrlNbr is not null && isIncumbent;

        if (Status == PositionSlotStatus.OnDuty
            || Status == PositionSlotStatus.OnDutyOvertime
            || Status == PositionSlotStatus.TiedUp
            || IsAnnulled
            || IsDoNotFill
            || IsSkipped)
        {
            return;
        }

        Status = employeeCtrlNbr is null
            ? PositionSlotStatus.Open
            : PositionSlotStatus.Filled;
    }

    public void MarkOnDuty()
    {
        Status = PositionSlotStatus.OnDuty;
    }

    public void MarkOnDutyOvertime()
    {
        Status = PositionSlotStatus.OnDutyOvertime;
    }

    public void MarkTiedUp()
    {
        Status = PositionSlotStatus.TiedUp;
    }

    public void MarkMarkedOff()
    {
        if (IsAnnulled || IsDoNotFill || IsSkipped)
            return;

        if (Status is PositionSlotStatus.OnDuty
            or PositionSlotStatus.OnDutyOvertime
            or PositionSlotStatus.TiedUp)
        {
            return;
        }

        Status = PositionSlotStatus.MarkedOff;
    }

    public void ClearMarkedOff()
    {
        if (Status != PositionSlotStatus.MarkedOff)
            return;

        if (IsAnnulled || IsDoNotFill || IsSkipped)
            return;

        if (IncumbentEmployeeCtrlNbr is null)
        {
            Status = PositionSlotStatus.Open;
            return;
        }

        Status = PositionSlotStatus.Filled;
    }

    public void Annul(string reason, DateTime annulmentDateTimeUtc)
    {
        IsAnnulled = true;
        AnnulmentReason = reason;
        AnnulmentDateTimeUtc = annulmentDateTimeUtc;
        Status = PositionSlotStatus.Annulled;
    }

    public void MarkDoNotFill()
    {
        IsDoNotFill = true;
        Status = PositionSlotStatus.DoNotFill;
    }

    public void Skip()
    {
        IsSkipped = true;
        Status = PositionSlotStatus.Skipped;
    }

    public void RestoreSlot()
    {
        IsAnnulled = false;
        AnnulmentReason = null;
        AnnulmentDateTimeUtc = null;
        IsDoNotFill = false;
        Status = IncumbentEmployeeCtrlNbr is not null
            ? PositionSlotStatus.Filled
            : PositionSlotStatus.Open;
    }

    internal static PositionSlotInstance CreateAdHoc(
        ControlNumber shiftInstanceCtrlNbr,
        int displayOrder,
        ControlNumber assignmentCtrlNbr,
        string assignmentCode,
        string assignmentName,
        string craftRoleName,
        string groupName,
        string groupCode,
        TimeOnly onDutyTime,
        TimeOnly offDutyTime,
        string crewName = "",
        string crewType = "")
    {
        return new PositionSlotInstance
        {
            ShiftInstanceCtrlNbr = shiftInstanceCtrlNbr,
            CrewPositionCtrlNbr = null,
            IncumbentEmployeeCtrlNbr = null,
            DisplayOrder = displayOrder,
            Status = PositionSlotStatus.Open,
            IsIncumbent = false,
            IsAdHoc = true,
            AssignmentCtrlNbr = assignmentCtrlNbr,
            AssignmentCode = assignmentCode,
            AssignmentName = assignmentName,
            CraftRoleName = craftRoleName,
            GroupName = groupName,
            GroupCode = groupCode,
            OnDutyTime = onDutyTime,
            OffDutyTime = offDutyTime,
            CrewName = crewName,
            CrewType = crewType
        };
    }

    internal void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }
}
