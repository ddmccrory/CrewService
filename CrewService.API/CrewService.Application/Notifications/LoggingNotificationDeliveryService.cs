using Microsoft.Extensions.Logging;

namespace CrewService.Application.Notifications;

/// <summary>
/// Stub <see cref="INotificationDeliveryService"/> that logs the delivery instead of pushing to
/// an external channel. Lets the full emission pipeline run end-to-end while the durable external
/// delivery (Teams/email/AtHoc) is built out in a later increment.
/// </summary>
public sealed class LoggingNotificationDeliveryService(ILogger<LoggingNotificationDeliveryService> logger)
    : INotificationDeliveryService
{
    public Task DeliverAsync(NotificationDeliveryRequest request, CancellationToken ct = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[NotificationDelivery STUB] Would deliver notification {Notification} to employee {Employee} " +
                "(railroad {Railroad}, category {Category}, requiresAck {RequiresAck}).",
                request.NotificationCtrlNbr,
                request.EmployeeCtrlNbr,
                request.RailroadCtrlNbr,
                request.Category,
                request.RequiresAcknowledgement);
        }

        return Task.CompletedTask;
    }
}
