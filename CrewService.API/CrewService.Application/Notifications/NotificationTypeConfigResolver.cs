using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Notifications;

public sealed class NotificationTypeConfigResolver(ILogger<NotificationTypeConfigResolver> logger)
{
    public async Task<NotificationTypeConfig?> ResolveAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber railroadCtrlNbr,
        string key,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var config = await uow.NotificationTypeConfigs.GetByRailroadAndKeyAsync(railroadCtrlNbr, key, ct);

        if (config is null)
        {
            logger.LogInformation(
                "Notification suppressed because config key {Key} was not found for railroad {RailroadCtrlNbr}.",
                key, railroadCtrlNbr.Value);
            return null;
        }

        if (!config.IsEnabled)
        {
            logger.LogInformation(
                "Notification suppressed because config key {Key} is disabled for railroad {RailroadCtrlNbr}.",
                key, railroadCtrlNbr.Value);
            return null;
        }

        return config;
    }
}
