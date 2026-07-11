using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Notifications;

/// <summary>
/// Read/acknowledge service for the recipient-facing notification surface. Resolves the
/// current authenticated user to their <see cref="Employee"/> record and exposes that
/// employee's notification history, the unacknowledged ("open") notices that drive the
/// legacy login-acknowledgement prompt, and an atomic electronic-acknowledge operation.
/// </summary>
public sealed class NotificationQueryService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    ICurrentUserService currentUserService)
{
    private async Task<Employee> ResolveCurrentEmployeeAsync(IOrchestrationUnitOfWork uow, CancellationToken ct)
    {
        var userId = currentUserService.GetUserId();
        if (userId == Guid.Empty)
            throw new InvalidOperationException("No authenticated user is available to resolve notifications for.");

        return await uow.Employees.GetByUserIdAsync(userId.ToString(), ct)
            ?? throw new InvalidOperationException(
                $"The current user '{userId}' is not linked to an employee record.");
    }

    // ── Read paths ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current employee's notifications, newest first (full history).
    /// </summary>
    public async Task<IReadOnlyList<EmployeeNotification>> GetMyNotificationsAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await ResolveCurrentEmployeeAsync(uow, ct);
        return await uow.EmployeeNotifications.GetByEmployeeAsync(employee.CtrlNbr, ct);
    }

    /// <summary>
    /// Returns the current employee's open notices — those requiring acknowledgement that
    /// have not yet been confirmed. Drives the login-acknowledgement prompt.
    /// </summary>
    public async Task<IReadOnlyList<EmployeeNotification>> GetMyUnacknowledgedAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await ResolveCurrentEmployeeAsync(uow, ct);
        return await uow.EmployeeNotifications.GetUnacknowledgedByEmployeeAsync(employee.CtrlNbr, ct);
    }

    /// <summary>
    /// Returns the count of the current employee's open (unacknowledged, ack-required)
    /// notices for the notification badge.
    /// </summary>
    public async Task<int> GetMyUnacknowledgedCountAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await ResolveCurrentEmployeeAsync(uow, ct);
        var open = await uow.EmployeeNotifications.GetUnacknowledgedByEmployeeAsync(employee.CtrlNbr, ct);
        return open.Count;
    }

    /// <summary>
    /// Returns every notification across the given railroad, newest first. Read-only
    /// reference feed for callers/managers/admins; recipient names are resolved at presentation.
    /// </summary>
    public async Task<IReadOnlyList<EmployeeNotification>> GetRailroadNotificationsAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeeNotifications.GetByRailroadAsync(railroadCtrlNbr, ct);
    }

    /// <summary>
    /// Returns the count of open (unacknowledged, ack-required) notices across the given
    /// railroad for the reference-menu badge.
    /// </summary>
    public async Task<int> GetRailroadUnacknowledgedCountAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeeNotifications.CountUnacknowledgedByRailroadAsync(railroadCtrlNbr, ct);
    }

    /// <summary>
    /// Returns a single employee's notifications, newest first. Read-only managerial review
    /// feed for the employee-detail Notifications tab; scoped server-side by employee.
    /// </summary>
    public async Task<IReadOnlyList<EmployeeNotification>> GetEmployeeNotificationsAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.EmployeeNotifications.GetByEmployeeAsync(employeeCtrlNbr, ct);
    }

    // ── Acknowledge ──────────────────────────────────────────────────────

    /// <summary>
    /// Records the current employee's electronic acknowledgement of one of their own notices,
    /// atomically in a single unit of work. Mirrors the legacy AcceptNotification action.
    /// Throws if the notice does not exist or does not belong to the current employee.
    /// </summary>
    public async Task<EmployeeNotification> AcknowledgeAsync(ControlNumber notificationCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employee = await ResolveCurrentEmployeeAsync(uow, ct);

        var notification = await uow.EmployeeNotifications.GetByCtrlNbrAsync(notificationCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Notification {notificationCtrlNbr.Value} not found.");

        if (notification.EmployeeCtrlNbr != employee.CtrlNbr)
            throw new UnauthorizedAccessException(
                $"Notification {notificationCtrlNbr.Value} does not belong to the current employee.");

        notification.AcknowledgeElectronically(currentUserService.GetUserName());
        uow.EmployeeNotifications.Update(notification);

        await TryScheduleHangoutAutoMoveAsync(uow, employee, notification, ct);
        await uow.CommitAsync(ct);

        return notification;
    }

    private static async Task TryScheduleHangoutAutoMoveAsync(
        IOrchestrationUnitOfWork uow,
        Employee employee,
        EmployeeNotification notification,
        CancellationToken ct)
    {
        if (notification.Category != NotificationCategories.BoardPlacement)
            return;

        var subject = notification.Subject;
        if (subject is null || subject.SubjectType != NotificationSubjectTypes.RosterBoard)
            return;

        var sourceBoard = await uow.RosterBoards.GetByCtrlNbrAsync(subject.SubjectCtrlNbr, ct);
        if (sourceBoard is null || sourceBoard.BoardType != BoardType.Hangout)
            return;

        var sourcePosition = sourceBoard.Positions.FirstOrDefault(p => p.EmployeeCtrlNbr == employee.CtrlNbr);
        if (sourcePosition is null)
            return;

        var craftPolicy = await uow.CraftOperationsPolicies.GetByCraftAsync(sourceBoard.CraftCtrlNbr, ct);
        if (craftPolicy is null || !craftPolicy.HangoutAutoMoveEnabled)
            return;

        if (!Enum.TryParse<BoardType>(craftPolicy.HangoutAutoMoveTargetBoardType, ignoreCase: true, out var targetBoardType))
            return;

        var craftBoards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(sourceBoard.CraftCtrlNbr, ct);
        var targetBoard = craftBoards.FirstOrDefault(b => b.IsActive && b.BoardType == targetBoardType);
        if (targetBoard is null)
            return;

        // Avoid duplicate active Hangout auto-moves for the same employee.
        var existingMoves = await uow.SeniorityMoves.GetByEmployeeAsync(employee.CtrlNbr, ct);
        if (existingMoves.Any(m =>
                m.MoveType == SeniorityMoveType.Hangout
                && (m.Status == SeniorityMoveStatus.Pending || m.Status == SeniorityMoveStatus.Approved)))
        {
            return;
        }

        var assignments = await uow.PositionAssignments.GetByEmployeeAsync(employee.CtrlNbr);
        var earliestAssignment = assignments.OrderBy(a => a.AssignedDateUtc).FirstOrDefault();
        var daysOnCurrentPosition = earliestAssignment is null
            ? 0
            : (int)(DateTime.UtcNow - earliestAssignment.AssignedDateUtc).TotalDays;

        var acceptedAtUtc = notification.Acknowledgements
            .Where(a => a.Confirmed)
            .OrderByDescending(a => a.NotifiedAtUtc)
            .Select(a => (DateTime?)a.NotifiedAtUtc)
            .FirstOrDefault() ?? DateTime.UtcNow;

        var delayHours = Math.Max(0, craftPolicy.HangoutAutoMoveDelayHours);
        var effectiveUtc = acceptedAtUtc.AddHours(delayHours);

        var nextOrder = targetBoard.Positions.Count > 0
            ? targetBoard.Positions.Max(p => p.PositionOrder) + 1
            : 1;

        var targetStaffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
        uow.StaffablePositions.Add(targetStaffablePosition);
        targetBoard.AddPosition(employee.CtrlNbr, nextOrder, targetStaffablePosition.CtrlNbr);
        uow.RosterBoards.Update(targetBoard);

        var move = SeniorityMove.Create(
            notification.RailroadCtrlNbr,
            employee.CtrlNbr,
            sourceBoard.CraftCtrlNbr,
            targetStaffablePosition.CtrlNbr,
            displacedEmployeeCtrlNbr: null,
            daysOnCurrentPosition,
            moveType: SeniorityMoveType.Hangout,
            effectiveUtc: effectiveUtc,
            willWork: null);

        await uow.SeniorityMoves.AddAsync(move, ct);
    }

    /// <summary>
    /// Records a manual (dispatcher) acknowledgement against any notice — e.g. the employee
    /// was reached by phone or verbally. Mirrors the legacy NotificationController.Notify action,
    /// capturing the contact method, phone number, notes, and whether contact was confirmed.
    /// Not restricted to the current employee since a dispatcher acts on behalf of others.
    /// </summary>
    public async Task<EmployeeNotification> RecordManualAcknowledgementAsync(
        ControlNumber notificationCtrlNbr,
        AcknowledgementMethod method,
        bool confirmed,
        string? phoneNumber = null,
        string? notes = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var notification = await uow.EmployeeNotifications.GetByCtrlNbrAsync(notificationCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Notification {notificationCtrlNbr.Value} not found.");

        notification.RecordAcknowledgement(
            method, confirmed, currentUserService.GetUserName(),
            notifiedAtUtc: DateTime.UtcNow, phoneNumber: phoneNumber, notes: notes);
        uow.EmployeeNotifications.Update(notification);
        await uow.CommitAsync(ct);

        return notification;
    }
}
