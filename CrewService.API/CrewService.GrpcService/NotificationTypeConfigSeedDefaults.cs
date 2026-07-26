using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.TenantConfig;

namespace CrewService.GrpcService;

public static class NotificationTypeConfigSeedDefaults
{
    private static readonly (string Key, string DisplayName, bool RequiresAcknowledgementDefault, string MessageTemplate)[] Defaults =
    [
        (NotificationCategories.BulletinAward, "Bulletin Award", true, "You have been awarded {position} effective {effective}."),
        (NotificationCategories.BulletinLost, "Bulletin Lost", false, "Your bid for {position} was not awarded."),
        (NotificationCategories.ForceAssign, "Force Assign", true, "You have been force-assigned to {position} effective {effective}."),
        (NotificationCategories.BulletinCancellation, "Bulletin Cancellation", false, "The bulletin for {position} has been cancelled and your bid is no longer active."),
        (NotificationCategories.SeniorityMove, "Seniority Move", true, "You have been assigned to {position} effective {effective}."),
        (NotificationCategories.SeniorityMoveCancelled, "Seniority Move Cancelled", false, "The seniority move that would have bumped you from {position} has been cancelled."),
        (NotificationCategories.PositionChange, "Position Change", true, "You will be bumped from {position}, by {byClause}, effective {effective}."),
        (NotificationCategories.BoardPlacement, "Board Placement", false, "You have been placed on {board}."),
        (NotificationCategories.WaitListPromotion, "Wait List Promotion", false, "Waitlist request was assigned. {absenceCode} absence request was created and approved for {datetime}."),
        (NotificationCategories.TieUp, "Tie-Up", true, "You have an outstanding on-duty record from {assignment} on duty at {onDuty} that requires completion."),
        (NotificationCategories.GeneralInformation, "General Information", false, "{message}")
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

        foreach (var (key, displayName, requiresAckDefault, messageTemplate) in Defaults)
        {
            var existingConfig = await notificationTypeConfigRepo.GetByRailroadAndKeyAsync(railroad.CtrlNbr, key, ct);
            if (existingConfig is not null)
            {
                if (string.IsNullOrWhiteSpace(existingConfig.MessageTemplate)
                    || string.Equals(existingConfig.MessageTemplate.Trim(), "{message}", StringComparison.OrdinalIgnoreCase))
                {
                    existingConfig.Update(
                        existingConfig.DisplayName,
                        existingConfig.IsEnabled,
                        existingConfig.RequiresAcknowledgementDefault,
                        existingConfig.Audience,
                        existingConfig.SendInApp,
                        existingConfig.SendEmail,
                        existingConfig.SendText,
                        existingConfig.SendExternalApi,
                        messageTemplate);

                    await notificationTypeConfigRepo.UpdateAsync(existingConfig, ct);
                }

                continue;
            }

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
                sendExternalApi: false,
                messageTemplate: messageTemplate),
                ct);
        }
    }

    public static async Task BackfillMessageTemplatesAsync(
        INotificationTypeConfigRepository notificationTypeConfigRepo,
        IEnumerable<DynamicGroup> railroads,
        CancellationToken ct = default)
    {
        foreach (var railroad in railroads)
        {
            foreach (var (key, _, _, messageTemplate) in Defaults)
            {
                var existingConfig = await notificationTypeConfigRepo.GetByRailroadAndKeyAsync(railroad.CtrlNbr, key, ct);
                if (existingConfig is null)
                    continue;

                if (!string.IsNullOrWhiteSpace(existingConfig.MessageTemplate)
                    && !string.Equals(existingConfig.MessageTemplate.Trim(), "{message}", StringComparison.OrdinalIgnoreCase)
                    && !(string.Equals(key, NotificationCategories.PositionChange, StringComparison.Ordinal)
                        && string.Equals(
                            existingConfig.MessageTemplate.Trim(),
                            "You will be bumped from {position}{byClause}, effective {effective}.",
                            StringComparison.Ordinal)
                        || string.Equals(
                            existingConfig.MessageTemplate.Trim(),
                            "You will be bumped from {position}, by {byClause}, effective {effective}.",
                            StringComparison.Ordinal)))
                {
                    continue;
                }

                existingConfig.Update(
                    existingConfig.DisplayName,
                    existingConfig.IsEnabled,
                    existingConfig.RequiresAcknowledgementDefault,
                    existingConfig.Audience,
                    existingConfig.SendInApp,
                    existingConfig.SendEmail,
                    existingConfig.SendText,
                    existingConfig.SendExternalApi,
                    messageTemplate);

                await notificationTypeConfigRepo.UpdateAsync(existingConfig, ct);
            }
        }
    }
}
