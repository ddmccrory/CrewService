using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Notifications;

public sealed class NotificationProviderConfig : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string ProviderType { get; private set; } = string.Empty;
    public string ConfigJson { get; private set; } = string.Empty;
    public int PollingIntervalSeconds { get; private set; }
    public int PollingTimeoutMinutes { get; private set; }
    public int BatchSize { get; private set; }
    public int BatchPauseSeconds { get; private set; }

    private NotificationProviderConfig() { WorkAreaGroupCtrlNbr = null!; }

    public static NotificationProviderConfig Create(
        ControlNumber workAreaGroupCtrlNbr,
        string providerType, string configJson,
        int pollingIntervalSeconds = 5, int pollingTimeoutMinutes = 6,
        int batchSize = 15, int batchPauseSeconds = 60)
    {
        return new NotificationProviderConfig
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            ProviderType = providerType,
            ConfigJson = configJson,
            PollingIntervalSeconds = pollingIntervalSeconds,
            PollingTimeoutMinutes = pollingTimeoutMinutes,
            BatchSize = batchSize,
            BatchPauseSeconds = batchPauseSeconds
        };
    }
}

public sealed class NotificationTypeConfig : Entity
{
    public ControlNumber RailroadCtrlNbr { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public bool RequiresAcknowledgementDefault { get; private set; }
    public NotificationAudience Audience { get; private set; }
    public bool SendInApp { get; private set; }
    public bool SendEmail { get; private set; }
    public bool SendText { get; private set; }
    public bool SendExternalApi { get; private set; }
    public string MessageTemplate { get; private set; } = string.Empty;

    private NotificationTypeConfig() { RailroadCtrlNbr = null!; }

    public static NotificationTypeConfig Create(
        ControlNumber railroadCtrlNbr,
        string key,
        string displayName,
        bool isEnabled,
        bool requiresAcknowledgementDefault,
        string messageTemplate,
        NotificationAudience audience = NotificationAudience.Employee,
        bool sendInApp = true,
        bool sendEmail = false,
        bool sendText = false,
        bool sendExternalApi = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var normalizedKey = key.Trim();
        var normalizedDisplayName = displayName.Trim();
        var isBoardPlacement = string.Equals(normalizedKey, NotificationCategories.BoardPlacement, StringComparison.Ordinal);
        var normalizedRequiresAcknowledgementDefault = isEnabled && !isBoardPlacement && requiresAcknowledgementDefault;
        var normalizedSendInApp = isEnabled && sendInApp;
        var normalizedSendEmail = isEnabled && sendEmail;
        var normalizedSendText = isEnabled && sendText;
        var normalizedSendExternalApi = isEnabled && sendExternalApi;

        if (isEnabled && !normalizedSendInApp && !normalizedSendEmail && !normalizedSendText && !normalizedSendExternalApi)
            throw new ArgumentException("At least one delivery option is required when the rule is enabled.", nameof(sendInApp));

        return new NotificationTypeConfig
        {
            RailroadCtrlNbr = railroadCtrlNbr,
            Key = normalizedKey,
            DisplayName = normalizedDisplayName,
            IsEnabled = isEnabled,
            RequiresAcknowledgementDefault = normalizedRequiresAcknowledgementDefault,
            Audience = audience,
            SendInApp = normalizedSendInApp,
            SendEmail = normalizedSendEmail,
            SendText = normalizedSendText,
            SendExternalApi = normalizedSendExternalApi,
            MessageTemplate = NormalizeMessageTemplate(messageTemplate)
        };
    }

    public void Update(
        string displayName,
        bool isEnabled,
        bool requiresAcknowledgementDefault,
        NotificationAudience audience,
        bool sendInApp,
        bool sendEmail,
        bool sendText,
        bool sendExternalApi,
        string messageTemplate)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        var isBoardPlacement = string.Equals(Key, NotificationCategories.BoardPlacement, StringComparison.Ordinal);
        var normalizedRequiresAcknowledgementDefault = isEnabled && !isBoardPlacement && requiresAcknowledgementDefault;
        var normalizedSendInApp = isEnabled && sendInApp;
        var normalizedSendEmail = isEnabled && sendEmail;
        var normalizedSendText = isEnabled && sendText;
        var normalizedSendExternalApi = isEnabled && sendExternalApi;

        if (isEnabled && !normalizedSendInApp && !normalizedSendEmail && !normalizedSendText && !normalizedSendExternalApi)
            throw new ArgumentException("At least one delivery option is required when the rule is enabled.", nameof(sendInApp));

        DisplayName = displayName.Trim();
        IsEnabled = isEnabled;
        RequiresAcknowledgementDefault = normalizedRequiresAcknowledgementDefault;
        Audience = audience;
        SendInApp = normalizedSendInApp;
        SendEmail = normalizedSendEmail;
        SendText = normalizedSendText;
        SendExternalApi = normalizedSendExternalApi;
        MessageTemplate = NormalizeMessageTemplate(messageTemplate);
    }

    private static string NormalizeMessageTemplate(string messageTemplate)
    {
        if (string.IsNullOrWhiteSpace(messageTemplate))
            throw new ArgumentException("Message template is required.", nameof(messageTemplate));

        var normalized = messageTemplate.Trim();
        if (normalized.Length == 0)
            throw new ArgumentException("Message template is required.", nameof(messageTemplate));

        return normalized;
    }
}
