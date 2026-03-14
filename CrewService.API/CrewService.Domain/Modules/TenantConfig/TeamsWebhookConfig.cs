using CrewService.Domain.Interfaces;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.TenantConfig;

public sealed class TeamsWebhookConfig : Entity
{
    public ControlNumber RailroadCtrlNbr { get; private set; }
    public ControlNumber? WorkAreaGroupCtrlNbr { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string WebhookUrl { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }

    private TeamsWebhookConfig()
    {
        RailroadCtrlNbr = null!;
    }

    private TeamsWebhookConfig(
        ControlNumber railroadCtrlNbr,
        ControlNumber? workAreaGroupCtrlNbr,
        NotificationChannel channel,
        string webhookUrl,
        bool isEnabled)
    {
        RailroadCtrlNbr = railroadCtrlNbr;
        WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr;
        Channel = channel;
        WebhookUrl = webhookUrl;
        IsEnabled = isEnabled;
    }

    public static TeamsWebhookConfig Create(
        ControlNumber railroadCtrlNbr,
        ControlNumber? workAreaGroupCtrlNbr,
        NotificationChannel channel,
        string webhookUrl,
        bool isEnabled)
    {
        return new TeamsWebhookConfig(
            railroadCtrlNbr,
            workAreaGroupCtrlNbr,
            channel,
            webhookUrl,
            isEnabled);
    }

    public void Update(string webhookUrl, bool isEnabled, string updatedBy)
    {
        WebhookUrl = webhookUrl;
        IsEnabled = isEnabled;
        ModifiedBy = AuditStamp.Create(updatedBy);
    }
}
