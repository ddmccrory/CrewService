using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Absence;

public sealed record AbsenceApprovalDecidedDomainEvent : DomainEvent
{
    public AbsenceApprovalDecidedDomainEvent(ControlNumber approvalCtrlNbr, ControlNumber requestCtrlNbr, string decision)
        : base("AbsenceApproval", approvalCtrlNbr.Value,
            payload: new { ApprovalCtrlNbr = approvalCtrlNbr.Value, RequestCtrlNbr = requestCtrlNbr.Value, Decision = decision }) { }
}

public sealed record AbsenceMarkUpScheduledDomainEvent : DomainEvent
{
    public AbsenceMarkUpScheduledDomainEvent(ControlNumber markUpCtrlNbr, ControlNumber requestCtrlNbr, DateTime scheduledUtc)
        : base("AbsenceMarkUp", markUpCtrlNbr.Value,
            payload: new { MarkUpCtrlNbr = markUpCtrlNbr.Value, RequestCtrlNbr = requestCtrlNbr.Value, ScheduledUtc = scheduledUtc }) { }
}

public sealed record AbsenceMarkedUpDomainEvent : DomainEvent
{
    public AbsenceMarkedUpDomainEvent(ControlNumber markUpCtrlNbr, ControlNumber employeeCtrlNbr)
        : base("AbsenceMarkUp", markUpCtrlNbr.Value,
            payload: new { MarkUpCtrlNbr = markUpCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value }) { }
}

public sealed record CompensationBalanceDebitedDomainEvent : DomainEvent
{
    public CompensationBalanceDebitedDomainEvent(ControlNumber balanceCtrlNbr, ControlNumber employeeCtrlNbr, decimal hours)
        : base("CompensationBalance", balanceCtrlNbr.Value,
            payload: new { BalanceCtrlNbr = balanceCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value, Hours = hours }) { }
}
