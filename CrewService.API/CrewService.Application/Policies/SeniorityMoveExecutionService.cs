using CrewService.Application.Notifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Policies;

/// <summary>
/// Executes an approved <see cref="SeniorityMove"/> by:
/// <list type="number">
///   <item>Completing the move (status → Completed).</item>
///   <item>Vacating the mover's current assignment (if any).</item>
///   <item>Assigning the mover to the target position.</item>
///   <item>For No Access moves: cancelling the mover's own pending moves and co-assigning
///         any open bulletin on the claimed position.</item>
///   <item>Placing the displaced employee (if any) on the craft's Hangout board.</item>
///   <item>Cancelling any other pending/approved moves targeting the same position.</item>
/// </list>
/// Seniority ordering: earlier <c>RosterDate</c> is more senior; for the same date,
/// the lower <c>Rank</c> number is more senior (Rank 1 is the most senior for that date).
/// </summary>
public sealed class SeniorityMoveExecutionService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    ILogger<SeniorityMoveExecutionService> logger,
    EmployeeNotificationService notifications)
{
    /// <summary>
    /// Executes a single approved seniority move identified by <paramref name="moveCtrlNbr"/>.
    /// </summary>
    public async Task ExecuteAsync(ControlNumber moveCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var move = await uow.SeniorityMoves.GetByCtrlNbrAsync(moveCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority move {moveCtrlNbr} not found.");

        if (move.Status != SeniorityMoveStatus.Approved)
        {
            logger.LogWarning("SeniorityMoveExecution: Move {MoveCtrlNbr} is in status '{Status}', skipping.", moveCtrlNbr, move.Status);
            return;
        }

        if (move.MoveType == SeniorityMoveType.Hangout)
        {
            var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(move.CraftCtrlNbr, ct);
            var hangoutBoard = boards.FirstOrDefault(b => b.IsActive && b.BoardType == BoardType.Hangout);
            var expectedSourcePosition = hangoutBoard?.Positions.FirstOrDefault(p => p.EmployeeCtrlNbr == move.EmployeeCtrlNbr);

            var currentAssignments = await uow.PositionAssignments.GetByEmployeeAsync(move.EmployeeCtrlNbr);
            var currentlyOnSource = expectedSourcePosition is not null
                && currentAssignments.Any(a => a.StaffablePositionCtrlNbr == expectedSourcePosition.StaffablePositionCtrlNbr);
            if (!currentlyOnSource)
            {
                move.Cancel("Hangout auto-move no longer applies because the employee is no longer in the source hangout position.");
                await uow.SeniorityMoves.UpdateAsync(move, ct);
                await uow.CommitAsync(ct);
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "SeniorityMoveExecution: Cancelled stale Hangout move {MoveCtrlNbr} for employee {Employee} because source hangout assignment is no longer active.",
                        moveCtrlNbr, move.EmployeeCtrlNbr);
                }
                return;
            }
        }

        // 1. Complete the move domain state
        move.Complete();
        await uow.SeniorityMoves.UpdateAsync(move, ct);

        // 2. Vacate the mover's current assignment (if any)
        var moverAssignments = await uow.PositionAssignments.GetByEmployeeAsync(move.EmployeeCtrlNbr);
        foreach (var assignment in moverAssignments)
        {
            assignment.Vacate();
            await uow.PositionAssignments.DeleteAsync(assignment.CtrlNbr, ct);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("SeniorityMoveExecution: Vacated employee {Employee} from position {Position}.",
                    move.EmployeeCtrlNbr, assignment.StaffablePositionCtrlNbr);
            }
        }

        // 3. Assign the mover to the target position
        //    Vacate whoever is currently on the target first (the displaced employee)
        var targetCurrentAssignment = await uow.PositionAssignments
            .GetByStaffablePositionAsync(move.TargetPositionCtrlNbr);

        ControlNumber? displacedEmployeeCtrlNbr = move.DisplacedEmployeeCtrlNbr;

        if (targetCurrentAssignment is not null)
        {
            // Prefer the live assignment over the recorded displaced field (same-day bumps, etc.)
            displacedEmployeeCtrlNbr ??= targetCurrentAssignment.EmployeeCtrlNbr;
            targetCurrentAssignment.Vacate();
            await uow.PositionAssignments.DeleteAsync(targetCurrentAssignment.CtrlNbr, ct);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("SeniorityMoveExecution: Vacated displaced employee {Displaced} from target position {Position}.",
                    displacedEmployeeCtrlNbr, move.TargetPositionCtrlNbr);
            }
        }

        var newAssignment = PositionAssignment.Create(
            move.TargetPositionCtrlNbr,
            move.EmployeeCtrlNbr,
            "SeniorityMove",
            assignmentSourceCtrlNbr: moveCtrlNbr);
        await uow.PositionAssignments.AddAsync(newAssignment, ct);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("SeniorityMoveExecution: Assigned employee {Employee} to position {Position}.",
                move.EmployeeCtrlNbr, move.TargetPositionCtrlNbr);
        }

        // 3b. No Access (administrative forced bump): cancel the mover's own remaining pending
        //     moves and co-assign any open bulletin on the claimed position.
        if (move.MoveType == SeniorityMoveType.NoAccess)
        {
            await CancelMoversPendingMovesAsync(uow, move.EmployeeCtrlNbr, moveCtrlNbr, ct);
            await CoAssignOpenBulletinAsync(uow, move.TargetPositionCtrlNbr, move.EmployeeCtrlNbr, ct);
        }

        // 4. Place the displaced employee on the craft Hangout board (if any)
        if (displacedEmployeeCtrlNbr is not null)
        {
            var placedBoard = await PlaceOnHangoutBoardAsync(uow, move.CraftCtrlNbr, displacedEmployeeCtrlNbr, ct);

            // Notify the displaced employee, honoring the board's tenant-configured placement policy.
            // Keep the seniority-move subject so the notice links back to the originating move.
            if (placedBoard is not null)
            {
                await notifications.NotifyBoardPlacementAsync(
                    uow, placedBoard, displacedEmployeeCtrlNbr,
                    Domain.Modules.Notifications.NotificationSubject.Create(
                        Domain.Modules.Notifications.NotificationSubjectTypes.SeniorityMove, moveCtrlNbr),
                    ct);
            }
        }

        // 5. Cancel other pending/approved moves targeting the same position
        await CancelCompetingMovesAsync(uow, move.TargetPositionCtrlNbr, move.EmployeeCtrlNbr, moveCtrlNbr, ct);

        // 6. Notify the mover that their move executed (position-affecting; requires acknowledgement).
        await notifications.NotifySeniorityMoveExecutedAsync(uow, move, ct);

        await uow.CommitAsync(ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "SeniorityMoveExecution: Move {MoveCtrlNbr} completed — employee {Employee} → position {Position}.",
                moveCtrlNbr, move.EmployeeCtrlNbr, move.TargetPositionCtrlNbr);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<RosterBoard?> PlaceOnHangoutBoardAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber craftCtrlNbr,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct)
    {
        var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(craftCtrlNbr, ct);
        var hangoutBoard = boards.FirstOrDefault(b => b.BoardType == BoardType.Hangout && b.IsActive);

        if (hangoutBoard is null)
        {
            logger.LogWarning(
                "SeniorityMoveExecution: No active Hangout board found for craft {Craft}. Displaced employee {Employee} not placed.",
                craftCtrlNbr, employeeCtrlNbr);
            return null;
        }

        // Check if the employee already has a position on this hangout board
        if (hangoutBoard.Positions.Any(p => p.EmployeeCtrlNbr == employeeCtrlNbr))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "SeniorityMoveExecution: Employee {Employee} already on Hangout board {Board}.",
                    employeeCtrlNbr, hangoutBoard.CtrlNbr);
            }
            return null;
        }

        // Create a StaffablePosition to back the hangout board slot
        var hangoutPosition = StaffablePosition.Create("Hangout");
        await uow.StaffablePositions.AddAsync(hangoutPosition, ct);

        var nextOrder = hangoutBoard.Positions.Count > 0
            ? hangoutBoard.Positions.Max(p => p.PositionOrder) + 1
            : 1;

        hangoutBoard.AddPosition(employeeCtrlNbr, nextOrder, hangoutPosition.CtrlNbr);
        await uow.RosterBoards.UpdateAsync(hangoutBoard, ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "SeniorityMoveExecution: Displaced employee {Employee} placed on Hangout board {Board}.",
                employeeCtrlNbr, hangoutBoard.CtrlNbr);
        }

        return hangoutBoard;
    }

    /// <summary>
    /// Cancels other pending/approved moves targeting the same position, but only those held by
    /// employees <em>junior</em> to the winner. More-senior rivals are left pending so they can
    /// still bump the winner when their own move executes. Mirrors SA's
    /// <c>RemoveOrNotifyValidSeniorityMoves</c>, which removes a competing move only when the
    /// winning employee <c>HasSeniority</c> over the rival and otherwise re-notifies it.
    /// Seniority: earlier <c>RosterDate</c> is more senior; for the same date, the lower
    /// <c>Rank</c> number is more senior.
    /// </summary>
    private async Task CancelCompetingMovesAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber targetPositionCtrlNbr,
        ControlNumber winnerEmployeeCtrlNbr,
        ControlNumber completedMoveCtrlNbr,
        CancellationToken ct)
    {
        var competing = await uow.SeniorityMoves
            .GetPendingByTargetPositionAsync(targetPositionCtrlNbr, completedMoveCtrlNbr, ct);

        // Also cancel any Approved-but-not-yet-due moves targeting this position
        var approvedCompeting = await uow.SeniorityMoves.GetByStatusAsync(SeniorityMoveStatus.Approved, ct);

        var allCompeting = competing
            .Concat(approvedCompeting.Where(m =>
                m.TargetPositionCtrlNbr == targetPositionCtrlNbr &&
                m.CtrlNbr != completedMoveCtrlNbr))
            .DistinctBy(m => m.CtrlNbr)
            .ToList();

        if (allCompeting.Count == 0) return;

        var winnerSeniority = await GetActiveSeniorityAsync(uow, winnerEmployeeCtrlNbr);

        foreach (var rival in allCompeting)
        {
            if (rival.Status != SeniorityMoveStatus.Pending && rival.Status != SeniorityMoveStatus.Approved)
                continue;

            // Leave moves held by employees who outrank the winner pending — a more-senior
            // rival can still claim this position when their move becomes due.
            var rivalSeniority = await GetActiveSeniorityAsync(uow, rival.EmployeeCtrlNbr);
            if (!WinnerOutranks(winnerSeniority, rivalSeniority))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "SeniorityMoveExecution: Retained competing move {Move} (employee {Employee}) for position {Position} — rival is senior to the winner.",
                        rival.CtrlNbr, rival.EmployeeCtrlNbr, targetPositionCtrlNbr);
                }
                continue;
            }

            rival.Cancel("Position was filled by a higher-seniority employee.");
            await uow.SeniorityMoves.UpdateAsync(rival, ct);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "SeniorityMoveExecution: Cancelled competing move {Move} (employee {Employee}) for position {Position}.",
                    rival.CtrlNbr, rival.EmployeeCtrlNbr, targetPositionCtrlNbr);
            }
        }
    }

    /// <summary>
    /// Returns the employee's active-roster <see cref="Domain.Models.Seniority.Seniority"/> entry,
    /// or <c>null</c> when none exists.
    /// </summary>
    private static async Task<Domain.Models.Seniority.Seniority?> GetActiveSeniorityAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr)
    {
        var entries = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        return entries.FirstOrDefault(s => s.LastActiveRoster);
    }

    /// <summary>
    /// True when <paramref name="winner"/> is more senior than <paramref name="rival"/>: earlier
    /// <c>RosterDate</c>, or the same date with a lower <c>Rank</c>. When seniority cannot be
    /// resolved for either side, defaults to <c>true</c> so the legacy "cancel competing moves"
    /// behaviour is preserved rather than leaving stale rival moves behind.
    /// </summary>
    private static bool WinnerOutranks(
        Domain.Models.Seniority.Seniority? winner,
        Domain.Models.Seniority.Seniority? rival)
    {
        if (winner is null || rival is null) return true;

        var dateCompare = winner.RosterDate.CompareTo(rival.RosterDate);
        if (dateCompare != 0) return dateCompare < 0;
        return winner.Rank < rival.Rank;
    }

    /// <summary>
    /// Cancels the moving employee's own remaining Pending/Approved moves when an administrative
    /// No Access bump is executed. Mirrors SA's <c>RailroadPoolEmployee.RemoveUnassignedSeniorityMoves</c>
    /// invoked for the bumping employee on <c>MoveType == "NA"</c>.
    /// </summary>
    private async Task CancelMoversPendingMovesAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber moverEmployeeCtrlNbr,
        ControlNumber completedMoveCtrlNbr,
        CancellationToken ct)
    {
        var moverMoves = await uow.SeniorityMoves.GetByEmployeeAsync(moverEmployeeCtrlNbr, ct);

        foreach (var pending in moverMoves)
        {
            if (pending.CtrlNbr == completedMoveCtrlNbr) continue;
            if (pending.Status != SeniorityMoveStatus.Pending && pending.Status != SeniorityMoveStatus.Approved)
                continue;

            pending.Cancel("Superseded by a No Access bump for the same employee.");
            await uow.SeniorityMoves.UpdateAsync(pending, ct);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "SeniorityMoveExecution: Cancelled mover's own move {Move} (employee {Employee}) due to No Access bump.",
                    pending.CtrlNbr, moverEmployeeCtrlNbr);
            }
        }
    }

    /// <summary>
    /// Co-assigns any open bulletin on the claimed crew position to the No Access mover, mirroring
    /// SA's <c>if (this.RailroadPosition.IsBulletined &amp;&amp; this.MoveType == "NA")</c> bulletin-assignment
    /// branch. Awards the bulletin to the mover and fills its vacancy so the bulletin worker stops
    /// processing it.
    /// </summary>
    private async Task CoAssignOpenBulletinAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber targetPositionCtrlNbr,
        ControlNumber moverEmployeeCtrlNbr,
        CancellationToken ct)
    {
        var vacancies = await uow.PositionVacancies
            .GetByTargetAsync(StaffablePositionType.Crew, targetPositionCtrlNbr);

        foreach (var vacancy in vacancies)
        {
            if (vacancy.Status is "Filled" or "Abolished") continue;

            var bulletin = await uow.Bulletins.GetByVacancyAsync(vacancy.CtrlNbr);
            if (bulletin is null) continue;

            // Only co-assign bulletins that are still open (not already awarded/closed out).
            if (bulletin.AwardedEmployeeCtrlNbr is not null ||
                bulletin.Status is "Completed" or "Cancelled")
                continue;

            bulletin.Award(moverEmployeeCtrlNbr);
            await uow.Bulletins.UpdateAsync(bulletin, ct);

            vacancy.Fill();
            await uow.PositionVacancies.UpdateAsync(vacancy, ct);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "SeniorityMoveExecution: Co-assigned open bulletin {Bulletin} on position {Position} to No Access mover {Employee}.",
                    bulletin.CtrlNbr, targetPositionCtrlNbr, moverEmployeeCtrlNbr);
            }
        }
    }
}
