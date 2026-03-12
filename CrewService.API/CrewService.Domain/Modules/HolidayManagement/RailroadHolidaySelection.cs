using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.HolidayManagement;

public sealed class RailroadHolidaySelection : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string HolidayCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private RailroadHolidaySelection() { WorkAreaGroupCtrlNbr = null!; }

    public static RailroadHolidaySelection Create(
        ControlNumber workAreaGroupCtrlNbr, string holidayCode, bool isActive = true)
    {
        return new RailroadHolidaySelection
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            HolidayCode = holidayCode,
            IsActive = isActive,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public void Activate() { IsActive = true; ModifiedBy = AuditStamp.Create("SYSTEM"); }
    public void Deactivate() { IsActive = false; ModifiedBy = AuditStamp.Create("SYSTEM"); }
}
