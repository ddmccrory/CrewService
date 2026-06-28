namespace CrewService.Domain.Modules.Notifications;

/// <summary>
/// How an employee was notified/acknowledged. Mirrors the legacy notification types
/// (Phone Call, Return Call, Called In, Verbal) plus the two system-generated methods.
/// </summary>
public enum AcknowledgementMethod
{
    PhoneCall = 0,
    ReturnCall = 1,
    CalledIn = 2,
    Verbal = 3,
    Automatic = 4,
    Electronic = 5
}
