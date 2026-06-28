using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Notifications;

public sealed record CrewCallSentDomainEvent : DomainEvent
{
    public CrewCallSentDomainEvent(ControlNumber requestCtrlNbr, ControlNumber employeeCtrlNbr, string templateType)
        : base("VacancyCallRequest", requestCtrlNbr.Value,
            payload: new { RequestCtrlNbr = requestCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value, TemplateType = templateType }) { }
}

public sealed record CrewCallRespondedDomainEvent : DomainEvent
{
    public CrewCallRespondedDomainEvent(ControlNumber requestCtrlNbr, string responseType)
        : base("VacancyCallRequest", requestCtrlNbr.Value,
            payload: new { RequestCtrlNbr = requestCtrlNbr.Value, ResponseType = responseType }) { }
}

public sealed record CrewCallExpiredDomainEvent : DomainEvent
{
    public CrewCallExpiredDomainEvent(ControlNumber requestCtrlNbr, ControlNumber employeeCtrlNbr)
        : base("VacancyCallRequest", requestCtrlNbr.Value,
            payload: new { RequestCtrlNbr = requestCtrlNbr.Value, EmployeeCtrlNbr = employeeCtrlNbr.Value }) { }
}

/// <summary>
/// Raised when an <c>EmployeeNotification</c> is created. Carries enough information for a
/// downstream delivery provider (Teams/email/AtHoc) to push the notice to the employee.
/// The in-app record is persisted atomically with the originating action; this event drives
/// the (currently stubbed) external delivery via the outbox.
/// </summary>
public sealed record EmployeeNotifiedDomainEvent : DomainEvent
{
    public EmployeeNotifiedDomainEvent(
        ControlNumber notificationCtrlNbr,
        ControlNumber railroadCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string category,
        bool requiresAcknowledgement)
        : base("EmployeeNotification", notificationCtrlNbr.Value,
            payload: new
            {
                NotificationCtrlNbr = notificationCtrlNbr.Value,
                RailroadCtrlNbr = railroadCtrlNbr.Value,
                EmployeeCtrlNbr = employeeCtrlNbr.Value,
                Category = category,
                RequiresAcknowledgement = requiresAcknowledgement
            }) { }
}
