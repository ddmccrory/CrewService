using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Notifications;

public sealed class NotificationTypeConfigAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<IReadOnlyList<NotificationTypeConfig>> GetByRailroadAsync(
        ControlNumber railroadCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.NotificationTypeConfigs.GetByRailroadAsync(railroadCtrlNbr, ct);
    }

    public async Task<NotificationTypeConfig> UpsertAsync(
        ControlNumber railroadCtrlNbr,
        string key,
        string displayName,
        bool isEnabled,
        bool requiresAcknowledgementDefault,
        NotificationAudience audience,
        bool sendInApp,
        bool sendEmail,
        bool sendText,
        bool sendExternalApi,
        string messageTemplate,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Notification type key is required.", nameof(key));

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var config = await uow.NotificationTypeConfigs.GetByRailroadAndKeyAsync(railroadCtrlNbr, key, ct);
        if (config is null)
        {
            config = NotificationTypeConfig.Create(
                railroadCtrlNbr,
                key,
                displayName,
                isEnabled,
                requiresAcknowledgementDefault,
                messageTemplate,
                audience,
                sendInApp,
                sendEmail,
                sendText,
                sendExternalApi);

            uow.NotificationTypeConfigs.Add(config);
        }
        else
        {
            config.Update(
                displayName,
                isEnabled,
                requiresAcknowledgementDefault,
                audience,
                sendInApp,
                sendEmail,
                sendText,
                sendExternalApi,
                messageTemplate);

            uow.NotificationTypeConfigs.Update(config);
        }

        await uow.CommitAsync(ct);
        return config;
    }
}
