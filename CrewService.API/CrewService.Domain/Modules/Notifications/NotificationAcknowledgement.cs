using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Notifications;

/// <summary>
/// A single notification/acknowledgement attempt against an <see cref="EmployeeNotification"/>.
/// Replaces the legacy ChangeNotification record.
/// </summary>
public sealed class NotificationAcknowledgement : Entity
{
    public ControlNumber EmployeeNotificationCtrlNbr { get; private set; }
    public DateTime NotifiedAtUtc { get; private set; }
    public AcknowledgementMethod Method { get; private set; }
    public bool Confirmed { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Notes { get; private set; }

    private NotificationAcknowledgement() { EmployeeNotificationCtrlNbr = null!; }

    internal static NotificationAcknowledgement Create(
        ControlNumber employeeNotificationCtrlNbr,
        AcknowledgementMethod method,
        bool confirmed,
        DateTime notifiedAtUtc,
        string? phoneNumber = null,
        string? notes = null)
    {
        return new NotificationAcknowledgement
        {
            EmployeeNotificationCtrlNbr = employeeNotificationCtrlNbr,
            Method = method,
            Confirmed = confirmed,
            NotifiedAtUtc = notifiedAtUtc,
            PhoneNumber = phoneNumber,
            Notes = notes
        };
    }
}
