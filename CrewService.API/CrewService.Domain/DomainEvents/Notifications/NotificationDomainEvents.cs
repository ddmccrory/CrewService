using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Notifications;

public sealed record CrewCallSentDomainEvent : DomainEvent
{
    public CrewCallSentDomainEvent(ControlNumber requestCtrlNbr, ControlNumber employeeCtrlNbr, string templateType)
        : base("NotificationRequest", requestCtrlNbr.Value,
            payload: new { RequestCtrlNbr = requestCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value, TemplateType = templateType }) { }
}

public sealed record CrewCallRespondedDomainEvent : DomainEvent
{
    public CrewCallRespondedDomainEvent(ControlNumber requestCtrlNbr, string responseType)
        : base("NotificationRequest", requestCtrlNbr.Value,
            payload: new { RequestCtrlNbr = requestCtrlNbr.Value, ResponseType = responseType }) { }
}

public sealed record CrewCallExpiredDomainEvent : DomainEvent
{
    public CrewCallExpiredDomainEvent(ControlNumber requestCtrlNbr, ControlNumber employeeCtrlNbr)
        : base("NotificationRequest", requestCtrlNbr.Value,
            payload: new { RequestCtrlNbr = requestCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value }) { }
}
