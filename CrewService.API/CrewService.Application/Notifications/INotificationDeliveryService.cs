namespace CrewService.Application.Notifications;

/// <summary>
/// Seam for delivering an <c>EmployeeNotification</c> to an external channel
/// (Teams, email, AtHoc, SMS, etc.). The in-app record is always persisted atomically
/// with the originating business action; this interface drives the separate, best-effort
/// external delivery triggered by the <c>EmployeeNotifiedDomainEvent</c> after commit.
/// </summary>
public interface INotificationDeliveryService
{
    Task DeliverAsync(NotificationDeliveryRequest request, CancellationToken ct = default);
}

/// <summary>
/// Carries the minimal information needed to deliver a notification to an external channel.
/// Populated from the <c>EmployeeNotifiedDomainEvent</c> payload.
/// </summary>
public sealed record NotificationDeliveryRequest(
    long NotificationCtrlNbr,
    long RailroadCtrlNbr,
    long EmployeeCtrlNbr,
    string Category,
    bool RequiresAcknowledgement);
