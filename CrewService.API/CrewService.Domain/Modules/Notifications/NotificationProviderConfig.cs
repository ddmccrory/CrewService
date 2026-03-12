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
            BatchPauseSeconds = batchPauseSeconds,
            CreatedBy = AuditStamp.Create("SYSTEM")
        };
    }
}
