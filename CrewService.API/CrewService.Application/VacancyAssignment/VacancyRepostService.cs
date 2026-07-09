using CrewService.Application.Bulletins;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.VacancyAssignment;

/// <summary>
/// Centralized policy for auto-bulletining vacated positions. This is the single source of
/// truth for the rule "any vacated crew position is auto-bulletined; an extra-board position
/// is auto-bulletined only when its occupancy falls below the board's RequiredPositions".
///
/// Two entry points:
/// <list type="bullet">
///   <item><see cref="RepostVacatedPositionAsync"/> — low-latency single-position handling,
///         invoked by the domain-event reactor after a position is vacated.</item>
///   <item><c>ReconcileUnbulletinedVacantPositionsAsync</c> — durable sweep invoked by the
///         BulletinProcessingWorker to catch anything the post-commit reactor missed.</item>
/// </list>
/// </summary>
public sealed class VacancyRepostService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    BulletinsService bulletinsService,
    ILogger<VacancyRepostService> logger) : IVacancyRepostService
{
    private const string CrewVacatedReason = "INCUMBENT_VACATED";
    private const string BoardUnderstaffedReason = "BOARD_UNDERSTAFFED";

    /// <summary>
    /// Evaluates a single vacated staffable position and, if it still requires staffing,
    /// opens a vacancy and posts a bulletin for it. Safe to call repeatedly (idempotent):
    /// positions that are already filled or already have an open/bulletined vacancy are skipped.
    /// </summary>
    public async Task RepostVacatedPositionAsync(
        ControlNumber staffablePositionCtrlNbr,
        ControlNumber? previousIncumbentCtrlNbr = null,
        CancellationToken ct = default)
    {
        var plan = await BuildRepostPlanAsync(staffablePositionCtrlNbr, ct);
        if (plan is null)
        {
            // No bulletin resulted from the vacate. For a board slot that means the board is still
            // adequately staffed (or otherwise not bulletinable), so the now-empty slot is surplus
            // capacity on a dynamically-sized board — remove it entirely. Crew positions are
            // structural and are never removed here.
            await RemoveSurplusBoardSlotIfPresentAsync(staffablePositionCtrlNbr, ct);
            return;
        }

        await bulletinsService.OpenVacancyAsync(
            workAreaGroupCtrlNbr: plan.WorkAreaGroupCtrlNbr,
            targetType: plan.TargetType,
            targetCtrlNbr: staffablePositionCtrlNbr,
            craftCtrlNbr: plan.CraftCtrlNbr,
            vacancyReasonCode: plan.VacancyReasonCode,
            previousIncumbentCtrlNbr: previousIncumbentCtrlNbr,
            targetName: plan.TargetName,
            ct: ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "VacancyRepost: Reposted {TargetType} position {Position} (reason {Reason}).",
                plan.TargetType, staffablePositionCtrlNbr.Value, plan.VacancyReasonCode);
        }
    }

    /// <summary>
    /// Removes a vacated extra-board slot that produced no repost bulletin (the board is still
    /// adequately staffed). Such a slot is surplus capacity, so the <see cref="RosterBoardPosition"/>
    /// is detached from its board and its backing <see cref="StaffablePosition"/> is removed. No-op
    /// when the vacated position is not a board slot (e.g. a structural crew position).
    /// </summary>
    private async Task RemoveSurplusBoardSlotIfPresentAsync(
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var board = await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(staffablePositionCtrlNbr, ct);
        if (board is null)
            return;

        var position = board.Positions.FirstOrDefault(p => p.StaffablePositionCtrlNbr == staffablePositionCtrlNbr);
        if (position is null)
            return;

        board.RemovePosition(position);
        uow.RosterBoards.Update(board);

        var staffablePosition = await uow.StaffablePositions.GetByCtrlNbrAsync(staffablePositionCtrlNbr, ct);
        if (staffablePosition is not null)
            uow.StaffablePositions.Remove(staffablePosition);

        await uow.CommitAsync(ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "VacancyRepost: Removed surplus board slot {Position} from board {Board} (board adequately staffed).",
                staffablePositionCtrlNbr.Value, board.CtrlNbr.Value);
        }
    }

    /// <summary>
    /// Reposts a specific extra-board slot when the board's occupancy has fallen below its
    /// RequiredPositions threshold. Used by board-management flows (e.g. removing a board
    /// position) where the position-to-board link is already severed, so the board must be
    /// supplied explicitly. No-op when the board is still adequately staffed or a vacancy
    /// already exists for the slot.
    /// </summary>
    public async Task RepostBoardPositionIfUnderstaffedAsync(
        ControlNumber boardCtrlNbr,
        ControlNumber vacatedStaffablePositionCtrlNbr,
        ControlNumber? previousIncumbentCtrlNbr = null,
        CancellationToken ct = default)
    {
        RepostPlan? plan;
        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            if (await HasOpenVacancyAsync(uow, StaffablePositionType.Board, vacatedStaffablePositionCtrlNbr))
                return;

            var board = await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr, ct);
            if (board is null)
                return;

            plan = await BuildBoardRepostPlanAsync(uow, board, vacatedStaffablePositionCtrlNbr, ct);
        }

        if (plan is null)
            return;

        await bulletinsService.OpenVacancyAsync(
            workAreaGroupCtrlNbr: plan.WorkAreaGroupCtrlNbr,
            targetType: plan.TargetType,
            targetCtrlNbr: vacatedStaffablePositionCtrlNbr,
            craftCtrlNbr: plan.CraftCtrlNbr,
            vacancyReasonCode: plan.VacancyReasonCode,
            previousIncumbentCtrlNbr: previousIncumbentCtrlNbr,
            targetName: plan.TargetName,
            ct: ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "VacancyRepost: Reposted understaffed board slot {Position} on board {Board}.",
                vacatedStaffablePositionCtrlNbr.Value, boardCtrlNbr.Value);
        }
    }

    /// <summary>
    /// Resolves whether a vacated staffable position should be reposted and, if so, the
    /// parameters needed to open the vacancy. Returns null when no repost is required
    /// (position refilled, vacancy already open, board still adequately staffed, or the
    /// position is not a bulletinable crew/board slot).
    /// </summary>
    private async Task<RepostPlan?> BuildRepostPlanAsync(
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        // Refill guard: the position currently has an active assignment — nothing to repost.
        var activeAssignment = await uow.PositionAssignments.GetByStaffablePositionAsync(staffablePositionCtrlNbr);
        if (activeAssignment is not null)
            return null;

        // Crew position path.
        var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(staffablePositionCtrlNbr);
        if (crewPosition is not null)
        {
            if (await HasOpenVacancyAsync(uow, StaffablePositionType.Crew, staffablePositionCtrlNbr))
                return null;

            var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
            var crew = await uow.Crews.GetByCtrlNbrAsync(crewPosition.CrewCtrlNbr, ct);
            if (craftRole is null || crew is null)
            {
                logger.LogWarning(
                    "VacancyRepost: Crew position {Position} missing craft role or crew — cannot repost.",
                    staffablePositionCtrlNbr.Value);
                return null;
            }

            if (!await HasBulletinRuleAsync(uow, craftRole.CraftCtrlNbr))
                return null;

            return new RepostPlan(
                WorkAreaGroupCtrlNbr: crew.WorkAreaCtrlNbr,
                TargetType: StaffablePositionType.Crew,
                CraftCtrlNbr: craftRole.CraftCtrlNbr,
                VacancyReasonCode: CrewVacatedReason,
                TargetName: VacancyTargetName.ForCrewPosition(crew, craftRole));
        }

        // Board position path: only repost when occupancy is below the board's RequiredPositions.
        var board = await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(staffablePositionCtrlNbr, ct);
        if (board is not null)
        {
            if (await HasOpenVacancyAsync(uow, StaffablePositionType.Board, staffablePositionCtrlNbr))
                return null;

            return await BuildBoardRepostPlanAsync(uow, board, staffablePositionCtrlNbr, ct);
        }

        return null;
    }

    /// <summary>
    /// Builds a board repost plan when the board's current occupancy (active assignments backing
    /// its slots) is below <see cref="Domain.Modules.Boards.RosterBoard.RequiredPositions"/>.
    /// Returns null when the board is adequately staffed or its work area cannot be resolved.
    /// </summary>
    private async Task<RepostPlan?> BuildBoardRepostPlanAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Boards.RosterBoard board,
        ControlNumber vacatedStaffablePositionCtrlNbr,
        CancellationToken ct)
    {
        if (board.RequiredPositions <= 0)
            return null;

        // Occupancy = board slots still backed by an active PositionAssignment. Measured by
        // assignment rows (not RosterBoardPosition rows) so a vacated-but-undeleted slot counts as open.
        var slotPositionCtrlNbrs = board.Positions
            .Select(p => p.StaffablePositionCtrlNbr)
            .Where(c => c != vacatedStaffablePositionCtrlNbr)
            .ToList();

        var occupied = slotPositionCtrlNbrs.Count == 0
            ? 0
            : (await uow.PositionAssignments.GetByStaffablePositionsAsync(slotPositionCtrlNbrs)).Count;

        if (occupied >= board.RequiredPositions)
            return null;

        var roster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr, ct);
        if (roster is null)
        {
            logger.LogWarning(
                "VacancyRepost: Board {Board} has no roster — cannot resolve work area for repost.",
                board.CtrlNbr.Value);
            return null;
        }

        if (!await HasBulletinRuleAsync(uow, board.CraftCtrlNbr))
            return null;

        return new RepostPlan(
            WorkAreaGroupCtrlNbr: roster.WorkAreaGroupCtrlNbr,
            TargetType: StaffablePositionType.Board,
            CraftCtrlNbr: board.CraftCtrlNbr,
            VacancyReasonCode: BoardUnderstaffedReason,
            TargetName: board.Name);
    }

    /// <summary>
    /// Durable reconciliation sweep. Finds vacant positions that were not reposted by the
    /// post-commit reactor (e.g. the process restarted before the fire-and-forget reaction ran)
    /// and reposts them. Invoked periodically by the BulletinProcessingWorker. Idempotent.
    /// </summary>
    public async Task<int> ReconcileUnbulletinedVacantPositionsAsync(CancellationToken ct = default)
    {
        var candidates = await GatherVacantCandidatesAsync(ct);

        var reposted = 0;
        foreach (var staffablePositionCtrlNbr in candidates)
        {
            try
            {
                await RepostVacatedPositionAsync(staffablePositionCtrlNbr, ct: ct);
                reposted++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "VacancyRepost: Reconciliation failed to repost position {Position}.",
                    staffablePositionCtrlNbr.Value);
            }
        }

        if (reposted > 0 && logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("VacancyRepost: Reconciliation reposted {Count} vacant position(s).", reposted);

        return reposted;
    }

    /// <summary>
    /// Collects the distinct staffable position control numbers that are currently vacant and
    /// may need reposting: all crew positions with no active assignment, plus extra-board slots
    /// whose backing assignment is gone. Per-position repost guards (idempotency, occupancy,
    /// refill) are re-applied by <see cref="RepostVacatedPositionAsync"/>.
    /// </summary>
    private async Task<IReadOnlyList<ControlNumber>> GatherVacantCandidatesAsync(CancellationToken ct)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var candidates = new List<ControlNumber>();

        // Crew: positions with no active PositionAssignment.
        candidates.AddRange(await uow.CrewPositions.GetVacantStaffablePositionCtrlNbrsAsync(ct));

        // Board: slots on RequiredPositions-enforcing boards whose backing assignment is gone.
        var boards = await uow.RosterBoards.GetAllAsync(ct);
        var boardSlotCtrlNbrs = boards
            .Where(b => b.IsActive && b.RequiredPositions > 0)
            .SelectMany(b => b.Positions.Select(p => p.StaffablePositionCtrlNbr))
            .Distinct()
            .ToList();

        if (boardSlotCtrlNbrs.Count > 0)
        {
            var assignedSlotCtrlNbrs = (await uow.PositionAssignments.GetByStaffablePositionsAsync(boardSlotCtrlNbrs))
                .Select(a => a.StaffablePositionCtrlNbr)
                .ToHashSet();

            candidates.AddRange(boardSlotCtrlNbrs.Where(c => !assignedSlotCtrlNbrs.Contains(c)));
        }

        return candidates;
    }

    /// <summary>
    /// True when an Open or Bulletined vacancy already exists for the target position
    /// (idempotency guard — avoids posting duplicate bulletins).
    /// </summary>
    private static async Task<bool> HasOpenVacancyAsync(
        IOrchestrationUnitOfWork uow,
        string targetType,
        ControlNumber targetCtrlNbr)
    {
        var existing = await uow.PositionVacancies.GetByTargetAsync(targetType, targetCtrlNbr);
        return existing.Any(v => v.Status is "Open" or "Bulletined");
    }

    /// <summary>
    /// True when the craft has a BulletinRule configured. Without one, OpenVacancyAsync would
    /// throw, so the repost is skipped gracefully (mirrors legacy "warn and skip" behavior).
    /// </summary>
    private async Task<bool> HasBulletinRuleAsync(IOrchestrationUnitOfWork uow, ControlNumber craftCtrlNbr)
    {
        var rule = await uow.BulletinRules.GetByCraftAsync(craftCtrlNbr);
        if (rule is null)
        {
            logger.LogWarning(
                "VacancyRepost: No BulletinRule configured for craft {Craft} — skipping repost.",
                craftCtrlNbr.Value);
            return false;
        }

        return true;
    }

    private sealed record RepostPlan(
        ControlNumber WorkAreaGroupCtrlNbr,
        string TargetType,
        ControlNumber CraftCtrlNbr,
        string VacancyReasonCode,
        string TargetName);
}
