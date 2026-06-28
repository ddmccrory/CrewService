using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Notifications;

/// <summary>
/// Application-layer notifier that creates recipient-centric <see cref="EmployeeNotification"/>
/// records inside the caller's existing unit of work, so each notice is persisted atomically
/// with the business action that triggered it (bulletin award, seniority move, etc.).
/// <para>
/// All message text is composed here (server-side) and the acknowledgement requirement is
/// decided centrally: position-affecting notices require acknowledgement; informational notices
/// (e.g. losing a bid) do not — mirroring the legacy login-acknowledgement behavior.
/// </para>
/// External delivery (Teams/email/AtHoc) is driven separately by the
/// <c>EmployeeNotifiedDomainEvent</c> raised on creation; it is currently stubbed.
/// </summary>
public sealed class EmployeeNotificationService(ILogger<EmployeeNotificationService> logger)
{
    /// <summary>
    /// Resolves the railroad (work-area <c>DynamicGroup</c>) that owns a bulletin via its vacancy.
    /// Bulletins are scoped to a railroad through the vacancy's work-area group.
    /// </summary>
    private static async Task<ControlNumber?> ResolveBulletinRailroadAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Bulletins.Bulletin bulletin,
        CancellationToken ct)
    {
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        if (vacancy is null) return null;

        var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(vacancy.WorkAreaGroupCtrlNbr, ct);
        if (workArea is null) return null;

        // A work-area group carries its owning railroad; if the group itself is the railroad,
        // RailroadCtrlNbr is null and the group's own CtrlNbr is the railroad.
        return workArea.RailroadCtrlNbr ?? workArea.CtrlNbr;
    }

    private EmployeeNotification Emit(
        IOrchestrationUnitOfWork uow,
        ControlNumber railroadCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string category,
        string message,
        bool requiresAcknowledgement,
        NotificationSubject? subject,
        DateTime? effectiveAtUtc)
    {
        var notification = EmployeeNotification.Create(
            railroadCtrlNbr,
            employeeCtrlNbr,
            category,
            message,
            requiresAcknowledgement,
            subject,
            effectiveAtUtc);

        uow.EmployeeNotifications.Add(notification);

        logger.LogInformation(
            "Notification queued: employee {Employee}, category {Category}, requiresAck {RequiresAck}.",
            employeeCtrlNbr.Value, category, requiresAcknowledgement);

        return notification;
    }

    // ── Bulletin notifications ───────────────────────────────────────────

    /// <summary>
    /// Notifies the employee that they won/were awarded a bulletin. Position-affecting, so it
    /// requires acknowledgement.
    /// </summary>
    public async Task NotifyBulletinAwardedAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Bulletins.Bulletin bulletin,
        ControlNumber employeeCtrlNbr,
        bool forceAssigned,
        CancellationToken ct = default)
    {
        var railroadCtrlNbr = await ResolveBulletinRailroadAsync(uow, bulletin, ct);
        if (railroadCtrlNbr is null)
        {
            logger.LogWarning(
                "Skipping bulletin-award notification for employee {Employee}: railroad could not be resolved for bulletin {Bulletin}.",
                employeeCtrlNbr.Value, bulletin.CtrlNbr.Value);
            return;
        }

        var category = forceAssigned ? NotificationCategories.ForceAssign : NotificationCategories.BulletinAward;
        var message = forceAssigned
            ? $"You have been force-assigned to a position effective {FormatEffective(bulletin.EffectiveUtc)}."
            : $"You have been awarded the bulletin position effective {FormatEffective(bulletin.EffectiveUtc)}.";

        var subject = NotificationSubject.Create(NotificationSubjectTypes.Bulletin, bulletin.CtrlNbr);

        Emit(uow, railroadCtrlNbr, employeeCtrlNbr, category, message,
            requiresAcknowledgement: true, subject, bulletin.EffectiveUtc);
    }

    /// <summary>
    /// Notifies an employee that they lost a bulletin bid. Informational only (legacy notified
    /// losers), so it does not require acknowledgement.
    /// </summary>
    public async Task NotifyBulletinLostAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Bulletins.Bulletin bulletin,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct = default)
    {
        var railroadCtrlNbr = await ResolveBulletinRailroadAsync(uow, bulletin, ct);
        if (railroadCtrlNbr is null)
        {
            logger.LogWarning(
                "Skipping bulletin-lost notification for employee {Employee}: railroad could not be resolved for bulletin {Bulletin}.",
                employeeCtrlNbr.Value, bulletin.CtrlNbr.Value);
            return;
        }

        var subject = NotificationSubject.Create(NotificationSubjectTypes.Bulletin, bulletin.CtrlNbr);

        Emit(uow, railroadCtrlNbr, employeeCtrlNbr, NotificationCategories.BulletinAward,
            "Your bid for a bulletin position was not awarded.",
            requiresAcknowledgement: false, subject, effectiveAtUtc: null);
    }

    // ── Seniority-move notifications ─────────────────────────────────────

    /// <summary>
    /// Notifies the moving employee that their seniority move has been executed. Position-affecting,
    /// so it requires acknowledgement.
    /// </summary>
    public Task NotifySeniorityMoveExecutedAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Policies.SeniorityMove move,
        CancellationToken ct = default)
    {
        var subject = NotificationSubject.Create(NotificationSubjectTypes.SeniorityMove, move.CtrlNbr);

        Emit(uow, move.RailroadCtrlNbr, move.EmployeeCtrlNbr, NotificationCategories.SeniorityMove,
            $"Your seniority move has been completed effective {FormatEffective(move.EffectiveUtc)}.",
            requiresAcknowledgement: true, subject, move.EffectiveUtc);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Notifies an employee that they were displaced from their position (by a bulletin award,
    /// force-assignment, or a higher-seniority move) and placed on the Hangout board.
    /// Position-affecting, so it requires acknowledgement.
    /// </summary>
    public Task NotifyDisplacedAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber railroadCtrlNbr,
        ControlNumber displacedEmployeeCtrlNbr,
        NotificationSubject? subject = null,
        CancellationToken ct = default)
    {
        Emit(uow, railroadCtrlNbr, displacedEmployeeCtrlNbr, NotificationCategories.PositionChange,
            "You have been displaced from your position and placed on the Hangout board.",
            requiresAcknowledgement: true, subject, effectiveAtUtc: null);

        return Task.CompletedTask;
    }

    private static string FormatEffective(DateTime? effectiveUtc) =>
        effectiveUtc.HasValue
            ? effectiveUtc.Value.ToString("g") + " UTC"
            : "immediately";
}
