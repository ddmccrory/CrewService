using CrewService.Domain.DomainEvents;
using CrewService.Domain.DomainEvents.Payroll;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Payroll;

public sealed class TimeEntry : Entity
{
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public DateTime DateUtc { get; private set; }
    public string EntryType { get; private set; } = string.Empty;
    public decimal Hours { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? Notes { get; private set; }
    public bool IsAdjustment { get; private set; }
    public ControlNumber? OriginalEntryCtrlNbr { get; private set; }

    private TimeEntry() { EmployeeCtrlNbr = null!; }

    public static TimeEntry Create(long employeeCtrlNbr, DateTime dateUtc, string entryType, decimal hours,
        string? reasonCode = null, string? notes = null, bool isAdjustment = false, long? originalEntryCtrlNbr = null)
    {
        return new TimeEntry
        {
            EmployeeCtrlNbr = ControlNumber.Create(employeeCtrlNbr),
            DateUtc = dateUtc,
            EntryType = entryType,
            Hours = hours,
            ReasonCode = reasonCode,
            Notes = notes,
            IsAdjustment = isAdjustment,
            OriginalEntryCtrlNbr = originalEntryCtrlNbr.HasValue ? ControlNumber.Create(originalEntryCtrlNbr.Value) : null
        };
    }
}

public sealed class PayrollRun : Entity
{
    public string PayPeriod { get; private set; } = string.Empty;
    public string Status { get; private set; } = "DRAFT";
    public DateTime? CalculatedAtUtc { get; private set; }
    public DateTime? LockedAtUtc { get; private set; }
    public int Version { get; private set; } = 1;

    private PayrollRun() { }

    public static PayrollRun Create(string payPeriod)
    {
        var run = new PayrollRun { PayPeriod = payPeriod };
        run.Raise(new PayrollCalculatedDomainEvent(run));
        return run;
    }

    public void MarkCalculated()
    {
        CalculatedAtUtc = DateTime.UtcNow;
        Raise(new PayrollCalculatedDomainEvent(this));
    }

    public void Lock()
    {
        Status = "LOCKED";
        LockedAtUtc = DateTime.UtcNow;
        Raise(new PayrollLockedDomainEvent(this));
        Raise(new PayrollPeriodLockedDomainEvent(CtrlNbr, PayPeriod));
    }

    public void Recalculate()
    {
        Version++;
        CalculatedAtUtc = DateTime.UtcNow;
        Status = "DRAFT";
    }
}

public sealed class PayrollRecord : Entity
{
    public ControlNumber PayrollRunCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string EarningsType { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public decimal Hours { get; private set; }
    public string? PolicyRef { get; private set; }
    public ControlNumber? OnDutyRecordCtrlNbr { get; private set; }
    public string? ResolvedEarningCode { get; private set; }
    public bool RequiresApproval { get; private set; }

    private PayrollRecord() { PayrollRunCtrlNbr = null!; EmployeeCtrlNbr = null!; }

    public static PayrollRecord Create(long payrollRunCtrlNbr, long employeeCtrlNbr, string earningsType,
        decimal amount, decimal hours, string? policyRef = null)
    {
        return new PayrollRecord
        {
            PayrollRunCtrlNbr = ControlNumber.Create(payrollRunCtrlNbr),
            EmployeeCtrlNbr = ControlNumber.Create(employeeCtrlNbr),
            EarningsType = earningsType,
            Amount = amount,
            Hours = hours,
            PolicyRef = policyRef
        };
    }

    public void SetEarningCode(string resolvedCode, bool requiresApproval, ControlNumber? onDutyRecordCtrlNbr = null)
    {
        ResolvedEarningCode = resolvedCode;
        RequiresApproval = requiresApproval;
        OnDutyRecordCtrlNbr = onDutyRecordCtrlNbr;
        Raise(new EarningCodeResolvedDomainEvent(CtrlNbr, resolvedCode, requiresApproval));
    }
}

public sealed class EarningApproval : Entity
{
    public ControlNumber PayrollRecordCtrlNbr { get; private set; }
    public int ApprovalTier { get; private set; }
    public ControlNumber OfficerCtrlNbr { get; private set; }
    public string Status { get; private set; } = "PENDING";
    public DateTime? DecidedAtUtc { get; private set; }

    private EarningApproval()
    {
        PayrollRecordCtrlNbr = null!;
        OfficerCtrlNbr = null!;
    }

    public static EarningApproval Create(
        ControlNumber payrollRecordCtrlNbr, int approvalTier, ControlNumber officerCtrlNbr)
    {
        return new EarningApproval
        {
            PayrollRecordCtrlNbr = payrollRecordCtrlNbr,
            ApprovalTier = approvalTier,
            OfficerCtrlNbr = officerCtrlNbr,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }

    public void Approve() { Status = "APPROVED"; DecidedAtUtc = DateTime.UtcNow; Raise(new EarningApprovalDecidedDomainEvent(CtrlNbr, "APPROVED")); }
    public void Decline() { Status = "DECLINED"; DecidedAtUtc = DateTime.UtcNow; Raise(new EarningApprovalDecidedDomainEvent(CtrlNbr, "DECLINED")); }
}

// Domain Events
public sealed record PayrollCalculatedDomainEvent : DomainEvent
{
    public PayrollCalculatedDomainEvent(PayrollRun r)
        : base(nameof(PayrollRun), r.CtrlNbr.Value, new { r.PayPeriod, r.Status, r.Version }) { }
}

public sealed record PayrollLockedDomainEvent : DomainEvent
{
    public PayrollLockedDomainEvent(PayrollRun r)
        : base(nameof(PayrollRun), r.CtrlNbr.Value, new { r.PayPeriod }) { }
}
