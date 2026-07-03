using CrewService.Application.Crews;
using CrewService.Application.RosterBoardOps;
using CrewService.Application.TenantConfig;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.SeniorityOps;

public sealed class SeniorityStateVacancyConfigService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    CrewsAppService crewsAppService,
    RosterBoardAppService rosterBoardAppService,
    IRailroadResolver railroadResolver,
    ILogger<SeniorityStateVacancyConfigService> logger)
{
    public async Task<List<SeniorityStateVacancyConfig>> GetByRailroadAsync(
        ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityStateVacancyConfigs.GetByRailroadCtrlNbrAsync(railroadCtrlNbr, ct);
    }

    public async Task<SeniorityStateVacancyConfig?> GetBySeniorityStateAsync(
        ControlNumber railroadCtrlNbr, ControlNumber seniorityStateCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityStateVacancyConfigs.GetBySeniorityStateAsync(railroadCtrlNbr, seniorityStateCtrlNbr, ct);
    }

    public async Task<SeniorityStateVacancyConfig> UpsertAsync(
        ControlNumber parentCtrlNbr,
        ControlNumber railroadCtrlNbr,
        ControlNumber seniorityStateCtrlNbr,
        VacancyAction vacancyAction,
        BoardType? targetBoardType = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.SeniorityStateVacancyConfigs
            .GetBySeniorityStateAsync(railroadCtrlNbr, seniorityStateCtrlNbr, ct);

        if (existing is not null)
        {
            existing.Update(vacancyAction, targetBoardType);
            uow.SeniorityStateVacancyConfigs.Update(existing);
            await uow.CommitAsync(ct);
            return existing;
        }

        var config = SeniorityStateVacancyConfig.Create(
            parentCtrlNbr, railroadCtrlNbr, seniorityStateCtrlNbr, vacancyAction, targetBoardType);
        uow.SeniorityStateVacancyConfigs.Add(config);
        await uow.CommitAsync(ct);
        return config;
    }

    public async Task DeleteAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var config = await uow.SeniorityStateVacancyConfigs.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"SeniorityStateVacancyConfig {ctrlNbr.Value} not found.");
        uow.SeniorityStateVacancyConfigs.Remove(config);
        await uow.CommitAsync(ct);
    }

    // ──────────────────────────────────────────────────────────────────
    // Action application — called when a seniority state changes
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the configured vacancy action for the new seniority state.
    /// Resolves the railroad from the employee's active roster, then executes
    /// the configured action (None / VacateAndBulletin / MoveToBoard).
    /// </summary>
    public async Task ApplyVacancyActionAsync(
        ControlNumber employeeCtrlNbr,
        ControlNumber newSeniorityStateCtrlNbr,
        ControlNumber rosterCtrlNbr,
        CancellationToken ct = default)
    {
        // ── Read phase ───────────────────────────────────────────────────────
        // Resolve the configured action, the positions to vacate, and (for MoveToBoard) the
        // target board using a single short-lived UoW. This UoW is fully disposed before any
        // canonical vacate/placement runs: those services each open their own transaction on the
        // shared connection, and SQLite does not support nested transactions.
        VacancyAction action;
        ControlNumber? targetBoardCtrlNbr = null;
        BoardType? targetBoardType = null;
        List<ControlNumber> crewIncumbencyCtrlNbrs;
        List<ControlNumber> boardPositionCtrlNbrs;

        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            var roster = await uow.Rosters.GetByCtrlNbrAsync(rosterCtrlNbr, ct);
            if (roster is null)
            {
                logger.LogWarning("ApplyVacancyAction: Roster {Roster} not found — skipping.", rosterCtrlNbr.Value);
                return;
            }

            // Resolve the railroad for the roster's work area. Vacancy configs are keyed by the
            // railroad group's CtrlNbr (the app-context railroad); the resolver handles both the
            // "work area references a railroad" and "railroad group is the work area" topologies.
            var railroadCtrlNbr = await railroadResolver.ResolveFromWorkAreaAsync(
                uow, roster.WorkAreaGroupCtrlNbr, ct);
            if (railroadCtrlNbr is null)
            {
                logger.LogWarning("ApplyVacancyAction: Work area {WorkArea} not found — skipping.", roster.WorkAreaGroupCtrlNbr.Value);
                return;
            }

            var config = await uow.SeniorityStateVacancyConfigs
                .GetBySeniorityStateAsync(railroadCtrlNbr, newSeniorityStateCtrlNbr, ct);

            if (config is null || config.VacancyAction == VacancyAction.None)
                return;

            action = config.VacancyAction;

            if (action == VacancyAction.MoveToBoard)
            {
                if (config.TargetBoardType is null)
                {
                    logger.LogWarning("ApplyVacancyAction (MoveToBoard): No target board type configured — skipping.");
                    return;
                }

                // Resolve the specific board by matching the employee's craft and the configured board type.
                if (roster.CraftCtrlNbr is null)
                {
                    logger.LogWarning("ApplyVacancyAction (MoveToBoard): Roster {Roster} has no craft — cannot resolve board.", rosterCtrlNbr.Value);
                    return;
                }

                var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(roster.CraftCtrlNbr, ct);
                var targetBoard = boards.FirstOrDefault(b =>
                    b.BoardType == config.TargetBoardType &&
                    b.IsActive);

                if (targetBoard is null)
                {
                    logger.LogWarning(
                        "ApplyVacancyAction (MoveToBoard): No active {BoardType} board found for craft {Craft}.",
                        config.TargetBoardType, roster.CraftCtrlNbr.Value);
                    return;
                }

                targetBoardCtrlNbr = targetBoard.CtrlNbr;
                targetBoardType = config.TargetBoardType;
            }

            (crewIncumbencyCtrlNbrs, boardPositionCtrlNbrs) =
                await CollectPositionsToVacateAsync(uow, employeeCtrlNbr, ct);
        }

        // ── Vacate phase ─────────────────────────────────────────────────────
        // Detach the employee from every current position. Each canonical vacate handles its own
        // bulletin/repost (crew positions always bulletin; extra boards repost when left
        // understaffed) inside its own UoW.
        await VacateResolvedPositionsAsync(crewIncumbencyCtrlNbrs, boardPositionCtrlNbrs, employeeCtrlNbr, ct);

        // ── Placement phase (MoveToBoard only) ───────────────────────────────
        // Now that the employee holds no staffable position (AddRosterBoardPositionAsync rejects
        // an employee who still holds one), place them at the end of the resolved target board.
        if (action == VacancyAction.MoveToBoard && targetBoardCtrlNbr is not null)
            await PlaceOnBoardAsync(targetBoardCtrlNbr, employeeCtrlNbr, targetBoardType, ct);
    }

    /// <summary>
    /// Read-phase resolution of the positions the employee currently holds, dispatched by the
    /// backing <see cref="Domain.Modules.Staffing.StaffablePosition.PositionType"/>. Crew
    /// positions are resolved to their active <see cref="Domain.Modules.Crews.CrewIncumbency"/>
    /// and board memberships to their <see cref="Domain.Modules.Boards.RosterBoardPosition"/>.
    /// Only CtrlNbrs are returned so the caller can dispose this UoW before invoking the canonical
    /// vacate services, each of which opens its own transaction on the shared connection.
    /// </summary>
    private async Task<(List<ControlNumber> CrewIncumbencyCtrlNbrs, List<ControlNumber> BoardPositionCtrlNbrs)>
        CollectPositionsToVacateAsync(
            IOrchestrationUnitOfWork uow,
            ControlNumber employeeCtrlNbr,
            CancellationToken ct)
    {
        var assignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        var crewIncumbencyCtrlNbrs = new List<ControlNumber>();
        var boardPositionCtrlNbrs = new List<ControlNumber>();

        foreach (var assignment in assignments)
        {
            var staffablePosition = await uow.StaffablePositions.GetByCtrlNbrAsync(assignment.StaffablePositionCtrlNbr, ct);
            if (staffablePosition is null)
                continue;

            if (staffablePosition.PositionType == StaffablePositionType.Crew)
            {
                var crewPosition = assignment.AssignmentSourceCtrlNbr is not null
                    ? await uow.CrewPositions.GetByCtrlNbrAsync(assignment.AssignmentSourceCtrlNbr, ct)
                    : await uow.CrewPositions.GetByStaffablePositionAsync(assignment.StaffablePositionCtrlNbr);
                if (crewPosition is null)
                    continue;

                var incumbency = await uow.CrewIncumbencies.GetActiveByPositionAsync(crewPosition.CtrlNbr, DateTime.UtcNow);
                if (incumbency is not null)
                    crewIncumbencyCtrlNbrs.Add(incumbency.CtrlNbr);
                else
                    logger.LogWarning(
                        "ApplyVacancyAction: Crew position {Position} for employee {Employee} has no active incumbency to end.",
                        crewPosition.CtrlNbr.Value, employeeCtrlNbr.Value);
            }
            else if (staffablePosition.PositionType == StaffablePositionType.Board)
            {
                var boardPositionCtrlNbr = assignment.AssignmentSourceCtrlNbr;
                if (boardPositionCtrlNbr is null)
                {
                    var board = await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(assignment.StaffablePositionCtrlNbr, ct);
                    boardPositionCtrlNbr = board?.Positions.FirstOrDefault(p => p.EmployeeCtrlNbr == employeeCtrlNbr)?.CtrlNbr;
                }

                if (boardPositionCtrlNbr is not null)
                    boardPositionCtrlNbrs.Add(boardPositionCtrlNbr);
                else
                    logger.LogWarning(
                        "ApplyVacancyAction: Board assignment for staffable position {Position} / employee {Employee} could not be resolved to a board position.",
                        assignment.StaffablePositionCtrlNbr.Value, employeeCtrlNbr.Value);
            }
        }

        return (crewIncumbencyCtrlNbrs, boardPositionCtrlNbrs);
    }

    /// <summary>
    /// Vacate-phase detachment of the resolved positions. Each canonical vacate opens and commits
    /// its own unit of work: <see cref="Crews.CrewsAppService.EndCrewIncumbencyAsync"/> ends the
    /// incumbency, removes the assignment, and auto-bulletins; and
    /// <see cref="RosterBoardOps.RosterBoardAppService.RemoveRosterBoardPositionAsync"/> removes the
    /// board position, removes the assignment, and reposts if the extra board is left understaffed.
    /// Runs outside any caller-held UoW so the shared SQLite connection has no open transaction.
    /// </summary>
    private async Task VacateResolvedPositionsAsync(
        IReadOnlyList<ControlNumber> crewIncumbencyCtrlNbrs,
        IReadOnlyList<ControlNumber> boardPositionCtrlNbrs,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct)
    {
        foreach (var incumbencyCtrlNbr in crewIncumbencyCtrlNbrs)
        {
            try
            {
                await crewsAppService.EndCrewIncumbencyAsync(incumbencyCtrlNbr, DateTime.UtcNow, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ApplyVacancyAction: Failed to end crew incumbency {Incumbency} for employee {Employee}.",
                    incumbencyCtrlNbr.Value, employeeCtrlNbr.Value);
            }
        }

        foreach (var boardPositionCtrlNbr in boardPositionCtrlNbrs)
        {
            try
            {
                await rosterBoardAppService.RemoveRosterBoardPositionAsync(boardPositionCtrlNbr, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ApplyVacancyAction: Failed to remove board position {Position} for employee {Employee}.",
                    boardPositionCtrlNbr.Value, employeeCtrlNbr.Value);
            }
        }
    }

    /// <summary>
    /// Placement-phase re-assignment of the employee onto the resolved target board (MoveToBoard
    /// action). Runs after the vacate phase has fully committed so the employee holds no staffable
    /// position, since <see cref="RosterBoardOps.RosterBoardAppService.AddRosterBoardPositionAsync"/>
    /// rejects an employee who is still assigned. The order is resolved inside a short-lived UoW to
    /// avoid nesting a transaction under the placement call.
    /// </summary>
    private async Task PlaceOnBoardAsync(
        ControlNumber targetBoardCtrlNbr,
        ControlNumber employeeCtrlNbr,
        BoardType? targetBoardType,
        CancellationToken ct)
    {
        int nextOrder;
        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            var board = await uow.RosterBoards.GetByCtrlNbrAsync(targetBoardCtrlNbr);
            if (board is null)
            {
                logger.LogWarning(
                    "ApplyVacancyAction (MoveToBoard): Target board {Board} no longer exists — cannot place employee {Employee}.",
                    targetBoardCtrlNbr.Value, employeeCtrlNbr.Value);
                return;
            }

            nextOrder = board.Positions.Count + 1;
        }

        try
        {
            await rosterBoardAppService.AddRosterBoardPositionAsync(targetBoardCtrlNbr, employeeCtrlNbr, nextOrder, ct);
            logger.LogInformation(
                "ApplyVacancyAction (MoveToBoard): Employee {Employee} placed on board {Board} ({BoardType}) at position {Order}.",
                employeeCtrlNbr.Value, targetBoardCtrlNbr.Value, targetBoardType, nextOrder);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ApplyVacancyAction (MoveToBoard): Failed to place employee {Employee} on board {Board}.",
                employeeCtrlNbr.Value, targetBoardCtrlNbr.Value);
        }
    }
}
