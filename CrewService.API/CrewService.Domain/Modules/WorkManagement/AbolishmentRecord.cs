using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.WorkManagement;

public sealed class AbolishmentRecord : Entity
{
    public ControlNumber TargetCtrlNbr { get; private set; }
    public string AbolishmentType { get; private set; } = string.Empty;
    public DateOnly EffectiveDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateOnly? RestoredDate { get; private set; }

    private AbolishmentRecord() { TargetCtrlNbr = null!; }

    public static AbolishmentRecord Create(
        ControlNumber targetCtrlNbr, string abolishmentType,
        DateOnly effectiveDate, string reason)
    {
        return new AbolishmentRecord
        {
            TargetCtrlNbr = targetCtrlNbr,
            AbolishmentType = abolishmentType,
            EffectiveDate = effectiveDate,
            Reason = reason,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public void Restore(DateOnly restoredDate)
    {
        RestoredDate = restoredDate;
        ModifiedBy = AuditStamp.Create("SYSTEM");
    }

    public bool IsActive(DateOnly asOf) =>
        asOf >= EffectiveDate && RestoredDate is null;
}
