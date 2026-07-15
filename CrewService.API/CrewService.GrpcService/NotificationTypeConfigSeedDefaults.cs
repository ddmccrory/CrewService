using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.TenantConfig;

namespace CrewService.GrpcService;

public static class NotificationTypeConfigSeedDefaults
{
    private static readonly (string Key, string DisplayName, bool RequiresAcknowledgementDefault)[] Defaults =
    [
        (NotificationCategories.BulletinAward, "Bulletin Award", true),
        (NotificationCategories.ForceAssign, "Force Assign", true),
        (NotificationCategories.BulletinCancellation, "Bulletin Cancellation", false),
        (NotificationCategories.SeniorityMove, "Seniority Move", true),
        (NotificationCategories.PositionChange, "Position Change", true),
        (NotificationCategories.BoardPlacement, "Board Placement", false),
        (NotificationCategories.GeneralInformation, "General Information", false)
    ];

    public static async Task SeedForRailroadsAsync(
        INotificationTypeConfigRepository notificationTypeConfigRepo,
        IEnumerable<DynamicGroup> railroads,
        Action<long>? setParent = null,
        CancellationToken ct = default)
    {
        foreach (var railroad in railroads)
            await SeedForRailroadAsync(notificationTypeConfigRepo, railroad, setParent, ct);
    }

    public static async Task SeedForRailroadAsync(
        INotificationTypeConfigRepository notificationTypeConfigRepo,
        DynamicGroup railroad,
        Action<long>? setParent = null,
        CancellationToken ct = default)
    {
        if (railroad.ParentCtrlNbr is not null)
            setParent?.Invoke(railroad.ParentCtrlNbr.Value);

        foreach (var (key, displayName, requiresAckDefault) in Defaults)
        {
            var existingConfig = await notificationTypeConfigRepo.GetByRailroadAndKeyAsync(railroad.CtrlNbr, key, ct);
            if (existingConfig is not null)
                continue;

            await notificationTypeConfigRepo.AddAsync(NotificationTypeConfig.Create(
                railroad.CtrlNbr,
                key,
                displayName,
                isEnabled: true,
                requiresAcknowledgementDefault: requiresAckDefault,
                audience: NotificationAudience.Employee,
                sendInApp: true,
                sendEmail: false,
                sendText: false,
                sendExternalApi: false),
                ct);
        }
    }
}
