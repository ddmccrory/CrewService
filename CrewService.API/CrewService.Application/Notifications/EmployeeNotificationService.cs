using CrewService.Application.Modules.UserAccount;
using CrewService.Application.Staffing;
using CrewService.Application.TenantConfig;
using CrewService.Application.Time;
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
public sealed class EmployeeNotificationService(
    ILogger<EmployeeNotificationService> logger,
    IRailroadResolver railroadResolver,
    NotificationTypeConfigResolver notificationTypeConfigResolver,
    IUserAccountService? userAccounts = null,
    IWorkAreaClock? clock = null)
{
    /// <summary>
    /// Resolves the railroad (work-area <c>DynamicGroup</c>) that owns a bulletin via its vacancy.
    /// Bulletins are scoped to a railroad through the vacancy's work-area group.
    /// </summary>
    private async Task<ControlNumber?> ResolveBulletinRailroadAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Bulletins.Bulletin bulletin,
        CancellationToken ct)
    {
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        if (vacancy is null) return null;

        // Bulletins are scoped to a railroad through the vacancy's work-area group; the resolver
        // handles both the "work area references a railroad" and "railroad group is the work area"
        // (small-railroad) topologies.
        return await railroadResolver.ResolveFromWorkAreaAsync(uow, vacancy.WorkAreaGroupCtrlNbr, ct);
    }

    private async Task<EmployeeNotification?> EmitAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber railroadCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string notificationTypeKey,
        string message,
        bool? requiresAcknowledgementOverride,
        NotificationSubject? subject,
        DateTime? effectiveAtUtc,
        CancellationToken ct)
    {
        var config = await notificationTypeConfigResolver.ResolveAsync(uow, railroadCtrlNbr, notificationTypeKey, ct);
        if (config is null)
            return null;

        var requiresAcknowledgement = requiresAcknowledgementOverride ?? config.RequiresAcknowledgementDefault;

        var notification = EmployeeNotification.Create(
            railroadCtrlNbr,
            employeeCtrlNbr,
            config.Key,
            message,
            requiresAcknowledgement,
            subject,
            effectiveAtUtc,
            audience: config.Audience,
            includeInHistory: true);

        uow.EmployeeNotifications.Add(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Notification queued: employee {Employee}, category {Category}, requiresAck {RequiresAck}.",
                employeeCtrlNbr.Value, config.Key, requiresAcknowledgement);
        }

        try
        {
            var projection = PositionChangeRecord.Create(
                railroadCtrlNbr,
                employeeCtrlNbr,
                sourceType: subject?.SubjectType ?? PositionChangeSourceTypes.Notification,
                sourceCtrlNbr: subject?.SubjectCtrlNbr,
                changeType: MapPositionChangeType(config.Key),
                message,
                requiresAcknowledgement,
                effectiveAtUtc,
                employeeNotificationCtrlNbr: notification.CtrlNbr);
            uow.PositionChangeRecords.Add(projection);
        }
        catch (NotSupportedException)
        {
            // Some focused test doubles and narrow UoW implementations intentionally omit
            // projection repositories; notification delivery must still succeed.
        }

        return notification;
    }

    private static string MapPositionChangeType(string category) => category switch
    {
        NotificationCategories.PositionChange => PositionChangeTypes.BumpRequested,
        NotificationCategories.SeniorityMove => PositionChangeTypes.MoveExecuted,
        NotificationCategories.BulletinAward => PositionChangeTypes.BulletinAwarded,
        NotificationCategories.ForceAssign => PositionChangeTypes.ForcedAssignment,
        NotificationCategories.BoardPlacement => PositionChangeTypes.BoardPlacement,
        _ => PositionChangeTypes.Informational
    };

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
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        var tz = vacancy is null ? null : await ResolveWorkAreaTimeZoneAsync(uow, vacancy.WorkAreaGroupCtrlNbr, ct);
        var positionName = vacancy?.TargetName ?? string.Empty;
        var positionClause = string.IsNullOrEmpty(positionName) ? "a position" : $"position {positionName}";
        var message = forceAssigned
            ? $"You have been force-assigned to {positionClause} effective {FormatEffectiveLocal(bulletin.EffectiveUtc, tz)}."
            : $"You have been awarded {positionClause} effective {FormatEffectiveLocal(bulletin.EffectiveUtc, tz)}.";

        var subject = NotificationSubject.Create(NotificationSubjectTypes.Bulletin, bulletin.CtrlNbr);

        await EmitAsync(uow, railroadCtrlNbr, employeeCtrlNbr, category, message,
            requiresAcknowledgementOverride: null, subject, bulletin.EffectiveUtc, ct);
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

        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        var positionName = vacancy?.TargetName ?? string.Empty;
        var positionClause = string.IsNullOrEmpty(positionName) ? "a position" : $"position {positionName}";

        await EmitAsync(uow, railroadCtrlNbr, employeeCtrlNbr, NotificationCategories.GeneralInformation,
            $"Your bid for {positionClause} was not awarded.",
            requiresAcknowledgementOverride: null, subject, effectiveAtUtc: null, ct);
    }

    /// <summary>
    /// Notifies a bidder that the bulletin they bid on was cancelled. Informational only
    /// (mirrors legacy RemoveRailroadPositionBulletin fanning out to each bidder), so it does
    /// not require acknowledgement.
    /// </summary>
    public async Task NotifyBulletinCancelledAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Bulletins.Bulletin bulletin,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct = default)
    {
        var railroadCtrlNbr = await ResolveBulletinRailroadAsync(uow, bulletin, ct);
        if (railroadCtrlNbr is null)
        {
            logger.LogWarning(
                "Skipping bulletin-cancelled notification for employee {Employee}: railroad could not be resolved for bulletin {Bulletin}.",
                employeeCtrlNbr.Value, bulletin.CtrlNbr.Value);
            return;
        }

        var subject = NotificationSubject.Create(NotificationSubjectTypes.Bulletin, bulletin.CtrlNbr);
        var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(bulletin.PositionVacancyCtrlNbr, ct);
        var positionName = vacancy?.TargetName ?? string.Empty;
        var positionClause = string.IsNullOrEmpty(positionName) ? "a position" : $"position {positionName}";

        await EmitAsync(uow, railroadCtrlNbr, employeeCtrlNbr, NotificationCategories.BulletinCancellation,
            $"The bulletin for {positionClause} has been cancelled and your bid is no longer active.",
            requiresAcknowledgementOverride: null, subject, effectiveAtUtc: null, ct);
    }

    // ── Seniority-move notifications ─────────────────────────────────────

    /// <summary>
    /// Notifies the moving employee that their seniority move has been executed. Position-affecting,
    /// so it requires acknowledgement.
    /// </summary>
    public async Task NotifySeniorityMoveExecutedAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Policies.SeniorityMove move,
        CancellationToken ct = default)
    {
        var subject = NotificationSubject.Create(NotificationSubjectTypes.SeniorityMove, move.CtrlNbr);

        var positionName = await StaffablePositionNameResolver.ResolveAsync(uow, move.TargetPositionCtrlNbr, ct);
        var positionClause = string.IsNullOrEmpty(positionName) ? "your position" : $"position {positionName}";

        var tz = await ResolvePositionTimeZoneAsync(uow, move.TargetPositionCtrlNbr, ct);

        await EmitAsync(uow, move.RailroadCtrlNbr, move.EmployeeCtrlNbr, NotificationCategories.SeniorityMove,
            $"You have been assigned to {positionClause} effective {FormatEffectiveLocal(move.EffectiveUtc, tz)}.",
            requiresAcknowledgementOverride: null, subject, move.EffectiveUtc, ct);
    }

    /// <summary>
    /// Notifies the soon-to-be-displaced employee that a seniority move targeting their position
    /// has been requested. Position-affecting, so it requires acknowledgement. Mirrors the legacy
    /// SeniorityMoveNotification raised at request time (not execution time), including the
    /// bumping employee's name.
    /// </summary>
    public async Task NotifySeniorityMoveRequestedAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Policies.SeniorityMove move,
        CancellationToken ct = default)
    {
        if (move.DisplacedEmployeeCtrlNbr is null)
            return;

        var subject = NotificationSubject.Create(NotificationSubjectTypes.SeniorityMove, move.CtrlNbr);

        var bumpingName = await ResolveEmployeeNameAsync(uow, move.EmployeeCtrlNbr, ct);
        var byClause = string.IsNullOrEmpty(bumpingName) ? string.Empty : $" by {bumpingName}";

        var positionName = await StaffablePositionNameResolver.ResolveAsync(uow, move.TargetPositionCtrlNbr, ct);
        var positionClause = string.IsNullOrEmpty(positionName) ? "your position" : $"position {positionName}";

        var tz = await ResolvePositionTimeZoneAsync(uow, move.TargetPositionCtrlNbr, ct);

        await EmitAsync(uow, move.RailroadCtrlNbr, move.DisplacedEmployeeCtrlNbr, NotificationCategories.PositionChange,
            $"You will be bumped from {positionClause}{byClause}, effective {FormatEffectiveLocal(move.EffectiveUtc, tz)}.",
            requiresAcknowledgementOverride: null, subject, move.EffectiveUtc, ct);
    }

    /// <summary>
    /// Notifies the previously-bumped employee that a seniority move targeting their position has
    /// been cancelled, and auto-completes the stale "you will be bumped" notice so it no longer
    /// prompts at login. Mirrors the legacy SeniorityMoveCancelNotification /
    /// CreateAutomaticChangeNotification behavior. Informational only.
    /// </summary>
    public async Task NotifySeniorityMoveCancelledAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Policies.SeniorityMove move,
        CancellationToken ct = default)
    {
        if (move.DisplacedEmployeeCtrlNbr is null)
            return;

        // Auto-complete the original ack-required bump notice so it doesn't linger after cancel.
        var open = await uow.EmployeeNotifications.GetUnacknowledgedByEmployeeAsync(move.DisplacedEmployeeCtrlNbr, ct);
        foreach (var stale in open.Where(n =>
            n.Subject is not null
            && n.Subject.SubjectType == NotificationSubjectTypes.SeniorityMove
            && n.Subject.SubjectCtrlNbr == move.CtrlNbr))
        {
            stale.RecordAcknowledgement(AcknowledgementMethod.Automatic, confirmed: true, acknowledgedByUser: "system");
            uow.EmployeeNotifications.Update(stale);

            var linked = await uow.EmployeeNotifications.GetOpenPositionChangesByNotificationAsync(stale.CtrlNbr, ct);
            foreach (var record in linked)
            {
                record.MarkSuperseded(PositionChangeClosedReasons.Cancelled);
                uow.PositionChangeRecords.Update(record);
            }
        }

        var subject = NotificationSubject.Create(NotificationSubjectTypes.SeniorityMove, move.CtrlNbr);
        var positionName = await StaffablePositionNameResolver.ResolveAsync(uow, move.TargetPositionCtrlNbr, ct);
        var positionClause = string.IsNullOrEmpty(positionName) ? "your position" : $"position {positionName}";

        await EmitAsync(uow, move.RailroadCtrlNbr, move.DisplacedEmployeeCtrlNbr, NotificationCategories.GeneralInformation,
            $"The seniority move that would have bumped you from {positionClause} has been cancelled.",
            requiresAcknowledgementOverride: null, subject, effectiveAtUtc: null, ct);

        var pendingByMove = await uow.PositionChangeRecords.GetOpenBySourceAsync(NotificationSubjectTypes.SeniorityMove, move.CtrlNbr, ct);
        foreach (var record in pendingByMove.Where(r => r.ChangeType == PositionChangeTypes.BumpRequested))
        {
            record.MarkSuperseded(PositionChangeClosedReasons.Cancelled);
            uow.PositionChangeRecords.Update(record);
        }
    }

    /// <summary>
    /// Resolves the work-area <see cref="TimeZoneInfo"/> that owns a target position via its crew,
    /// so effective times render in the work-area's local zone. Returns <c>null</c> (UTC) when the
    /// clock is unavailable or the position is not crew-scoped.
    /// </summary>
    private async Task<TimeZoneInfo?> ResolvePositionTimeZoneAsync(
        IOrchestrationUnitOfWork uow, ControlNumber targetPositionCtrlNbr, CancellationToken ct)
    {
        if (clock is null) return null;
        var crewPos = await uow.CrewPositions.GetByStaffablePositionAsync(targetPositionCtrlNbr);
        if (crewPos is null) return null;
        return await clock.GetCrewTimeZoneAsync(uow, crewPos.CrewCtrlNbr, ct);
    }

    /// <summary>
    /// Resolves the work-area <see cref="TimeZoneInfo"/> for a work-area group, so effective times
    /// render in the work-area's local zone. Returns <c>null</c> (UTC) when the clock is unavailable.
    /// </summary>
    private async Task<TimeZoneInfo?> ResolveWorkAreaTimeZoneAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber workAreaGroupCtrlNbr,
        CancellationToken ct)
    {
        if (clock is null) return null;
        return await clock.GetWorkAreaTimeZoneAsync(uow, workAreaGroupCtrlNbr, ct);
    }

    /// <summary>
    /// Resolves an employee's display name (<c>LastName, FirstName M.</c>) via their Identity user
    /// profile. Returns <see cref="string.Empty"/> when the employee, user, or account service is
    /// unavailable so the message degrades gracefully.
    /// </summary>
    private async Task<string> ResolveEmployeeNameAsync(
        IOrchestrationUnitOfWork uow, ControlNumber employeeCtrlNbr, CancellationToken ct)
    {
        if (userAccounts is null) return string.Empty;

        var employee = await uow.Employees.GetByCtrlNbrAsync(employeeCtrlNbr, ct);
        if (employee is null || string.IsNullOrEmpty(employee.UserId)) return string.Empty;

        var names = await userAccounts.GetNamesByIdsAsync([employee.UserId]);
        return names.Count > 0 ? names[0].FullNameLNF ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Notifies an employee that they were placed on a board, honoring the board's tenant-configured
    /// placement-notification policy. Fires for <em>any</em> placement path (manual add, seniority
    /// move displacement, or seniority-state change): the caller passes the board and the affected
    /// employee, and the per-board <see cref="Domain.Modules.Boards.RosterBoard.NotifyOnPlacement"/> /
    /// <see cref="Domain.Modules.Boards.RosterBoard.PlacementRequiresAcknowledgement"/> flags decide
    /// whether a notice is raised and whether acknowledgement is required. Emulates SA's
    /// hangout-placement notification without hardcoding board types. No-op when the board opts out.
    /// </summary>
    public async Task NotifyBoardPlacementAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Boards.RosterBoard board,
        ControlNumber employeeCtrlNbr,
        NotificationSubject? subject = null,
        CancellationToken ct = default)
    {
        if (!board.NotifyOnPlacement)
            return;

        var railroadCtrlNbr = await ResolveBoardRailroadAsync(uow, board, ct);
        if (railroadCtrlNbr is null)
        {
            logger.LogWarning(
                "Skipping board-placement notification for employee {Employee}: railroad could not be resolved for board {Board}.",
                employeeCtrlNbr.Value, board.CtrlNbr.Value);
            return;
        }

        var placementSubject = subject
            ?? NotificationSubject.Create(NotificationSubjectTypes.RosterBoard, board.CtrlNbr);
        var boardClause = string.IsNullOrWhiteSpace(board.Name) ? "a board" : $"the {board.Name} board";

        await EmitAsync(uow, railroadCtrlNbr, employeeCtrlNbr, NotificationCategories.BoardPlacement,
            $"You have been placed on {boardClause}.",
            requiresAcknowledgementOverride: board.PlacementRequiresAcknowledgement, placementSubject, effectiveAtUtc: null, ct);
    }

    /// <summary>
    /// Resolves the railroad (work-area <c>DynamicGroup</c>) that owns a roster board via its
    /// roster's work-area group. Returns <c>null</c> when the board has no roster/work-area so the
    /// caller can skip the notification.
    /// </summary>
    private async Task<ControlNumber?> ResolveBoardRailroadAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Boards.RosterBoard board,
        CancellationToken ct)
    {
        if (board.RosterCtrlNbr is null)
            return null;

        var roster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr, ct);
        if (roster is null)
            return null;

        return await railroadResolver.ResolveFromWorkAreaAsync(uow, roster.WorkAreaGroupCtrlNbr, ct);
    }

    private static string FormatEffectiveLocal(DateTime? effectiveUtc, TimeZoneInfo? tz)
    {
        if (!effectiveUtc.HasValue) return "immediately";
        var utc = DateTime.SpecifyKind(effectiveUtc.Value, DateTimeKind.Utc);
        var local = tz is null ? utc : TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        return local.ToString("MM/dd/yyyy HH:mm");
    }
}
