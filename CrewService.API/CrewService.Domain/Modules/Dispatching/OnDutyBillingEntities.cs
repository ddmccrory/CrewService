using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Dispatching;

public sealed class OnDutyBillingRecord : Entity
{
    public ControlNumber OnDutyRecordCtrlNbr { get; private set; }
    public string BillingType { get; private set; } = string.Empty;
    public string BillingCode { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public decimal Hours { get; private set; }
    public string? Description { get; private set; }

    private OnDutyBillingRecord() { OnDutyRecordCtrlNbr = null!; }

    public static OnDutyBillingRecord Create(
        ControlNumber onDutyRecordCtrlNbr, string billingType,
        string billingCode, decimal amount, decimal hours, string? description)
    {
        return new OnDutyBillingRecord
        {
            OnDutyRecordCtrlNbr = onDutyRecordCtrlNbr,
            BillingType = billingType,
            BillingCode = billingCode,
            Amount = amount,
            Hours = hours,
            Description = description,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }
}

public sealed class OnDutyLocomotiveRecord : Entity
{
    public ControlNumber OnDutyRecordCtrlNbr { get; private set; }
    public string LocomotiveNumber { get; private set; } = string.Empty;
    public string LocomotiveTypeCode { get; private set; } = string.Empty;
    public decimal Hours { get; private set; }

    private OnDutyLocomotiveRecord() { OnDutyRecordCtrlNbr = null!; }

    public static OnDutyLocomotiveRecord Create(
        ControlNumber onDutyRecordCtrlNbr, string locomotiveNumber,
        string locomotiveTypeCode, decimal hours)
    {
        return new OnDutyLocomotiveRecord
        {
            OnDutyRecordCtrlNbr = onDutyRecordCtrlNbr,
            LocomotiveNumber = locomotiveNumber,
            LocomotiveTypeCode = locomotiveTypeCode,
            Hours = hours,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }
}

public sealed class OnDutyMaterialRecord : Entity
{
    public ControlNumber OnDutyRecordCtrlNbr { get; private set; }
    public string MaterialCode { get; private set; } = string.Empty;
    public string CategoryCode { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }

    private OnDutyMaterialRecord() { OnDutyRecordCtrlNbr = null!; }

    public static OnDutyMaterialRecord Create(
        ControlNumber onDutyRecordCtrlNbr, string materialCode,
        string categoryCode, decimal quantity, decimal unitCost)
    {
        return new OnDutyMaterialRecord
        {
            OnDutyRecordCtrlNbr = onDutyRecordCtrlNbr,
            MaterialCode = materialCode,
            CategoryCode = categoryCode,
            Quantity = quantity,
            UnitCost = unitCost,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }
}
