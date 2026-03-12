using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Dispatching;

public sealed class ChangeNotification : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string ChangeType { get; private set; } = string.Empty;
    public DateOnly EffectiveDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public DateTime CreatedAtUtc { get; private set; }

    private ChangeNotification()
    {
        WorkAreaGroupCtrlNbr = null!;
    }

    private ChangeNotification(
        ControlNumber workAreaGroupCtrlNbr,
        string changeType,
        DateOnly effectiveDate,
        string description)
    {
        WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr;
        ChangeType = changeType;
        EffectiveDate = effectiveDate;
        Description = description;
        Status = "Pending";
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = AuditStamp.Create("SYSTEM");
    }

    public static ChangeNotification Create(
        ControlNumber workAreaGroupCtrlNbr,
        string changeType,
        DateOnly effectiveDate,
        string description)
    {
        return new ChangeNotification(workAreaGroupCtrlNbr, changeType, effectiveDate, description);
    }

    public void Apply(string appliedBy)
    {
        Status = "Applied";
        ModifiedBy = AuditStamp.Create(appliedBy);
    }

    public void Cancel(string cancelledBy)
    {
        Status = "Cancelled";
        ModifiedBy = AuditStamp.Create(cancelledBy);
    }
}
