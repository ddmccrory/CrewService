using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Absence;

public sealed record AbsenceApprovalDecidedDomainEvent : DomainEvent
{
    public AbsenceApprovalDecidedDomainEvent(ControlNumber approvalCtrlNbr, ControlNumber requestCtrlNbr, string decision)
        : base("AbsenceApproval", approvalCtrlNbr.Value,
            payload: new { ApprovalCtrlNbr = approvalCtrlNbr.Value, RequestCtrlNbr = requestCtrlNbr.Value, Decision = decision }) { }
}

public sealed record AbsenceEndedDomainEvent : DomainEvent
{
    public AbsenceEndedDomainEvent(ControlNumber endRecordCtrlNbr, ControlNumber requestCtrlNbr)
        : base("AbsenceEndRecord", endRecordCtrlNbr.Value,
            payload: new { EndRecordCtrlNbr = endRecordCtrlNbr.Value, RequestCtrlNbr = requestCtrlNbr.Value }) { }
}

public sealed record CompensationBalanceDebitedDomainEvent : DomainEvent
{
    public CompensationBalanceDebitedDomainEvent(ControlNumber balanceCtrlNbr, ControlNumber employeeCtrlNbr, decimal hours)
        : base("CompensationBalance", balanceCtrlNbr.Value,
            payload: new { BalanceCtrlNbr = balanceCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value, Hours = hours }) { }
}
