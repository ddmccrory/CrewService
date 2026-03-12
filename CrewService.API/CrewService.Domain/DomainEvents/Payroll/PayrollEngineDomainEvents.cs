using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Payroll;

public sealed record EarningCodeResolvedDomainEvent : DomainEvent
{
    public EarningCodeResolvedDomainEvent(ControlNumber recordCtrlNbr, string earningCode, bool requiresApproval)
        : base("PayrollRecord", recordCtrlNbr.Value,
            payload: new { RecordCtrlNbr = recordCtrlNbr.Value, EarningCode = earningCode, RequiresApproval = requiresApproval }) { }
}

public sealed record EarningApprovalDecidedDomainEvent : DomainEvent
{
    public EarningApprovalDecidedDomainEvent(ControlNumber approvalCtrlNbr, string decision)
        : base("EarningApproval", approvalCtrlNbr.Value,
            payload: new { ApprovalCtrlNbr = approvalCtrlNbr.Value, Decision = decision }) { }
}

public sealed record PayrollPeriodLockedDomainEvent : DomainEvent
{
    public PayrollPeriodLockedDomainEvent(ControlNumber runCtrlNbr, string payPeriod)
        : base("PayrollRun", runCtrlNbr.Value,
            payload: new { RunCtrlNbr = runCtrlNbr.Value, PayPeriod = payPeriod }) { }
}
