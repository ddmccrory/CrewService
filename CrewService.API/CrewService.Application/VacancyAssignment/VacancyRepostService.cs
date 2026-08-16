using CrewService.Application.Bulletins;
using CrewService.Application.Workflows;
using CrewService.Application.Workflows.Effects;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
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
    ILogger<VacancyRepostService> logger,
    IServiceProvider? serviceProvider = null) : IVacancyRepostService
{
    private const string CrewVacatedReason = "INCUMBENT_VACATED";
    private const string BoardUnderstaffedReason = "BOARD_UNDERSTAFFED";
    private const string IncumbentRemovedReason = "INCUMBENT_REMOVED";
    private const string VacatedTargetCancellationReason = "Cancelled because target position no longer has an incumbent and is being filled through bulletin posting.";

    /// <summary>
    /// Evaluates a single vacated staffable position and, if it still requires staffing,
    /// opens a vacancy and posts a bulletin for it. Safe to call repeatedly (idempotent):
    /// positions that are already filled or already have an open/bulletined vacancy are skipped.
    /// </summary>
    public async Task RepostVacatedPositionAsync(
        ControlNumber staffablePositionCtrlNbr,
        ControlNumber? previousIncumbentCtrlNbr = null,
        CancellationToken ct = default,
        bool executeWorkflowTrigger = true)
    {
        await CancelMovesForVacatedTargetAsync(staffablePositionCtrlNbr, ct);

        if (executeWorkflowTrigger)
        {
            await TryExecutePositionVacatedWorkflowAsync(
                staffablePositionCtrlNbr,
                previousIncumbentCtrlNbr,
                ct: ct);
            return;
        }

        var decision = await ResolveRepostDecisionAsync(staffablePositionCtrlNbr, ct);
        switch (decision.Action)
        {
            case RepostAction.NoAction:
                return;

            case RepostAction.OpenVacancyAndBulletin:
            {
                var plan = decision.Plan
                    ?? throw new InvalidOperationException("Open-vacancy repost action requires a repost plan.");

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

                return;
            }

            case RepostAction.PostExistingOpenVacancy:
            {
                var vacancyCtrlNbr = decision.ExistingVacancyCtrlNbr
                    ?? throw new InvalidOperationException("Post-existing-vacancy action requires a vacancy control number.");

                await PostExistingOpenVacancyAsync(vacancyCtrlNbr, staffablePositionCtrlNbr, ct);
                return;
            }

            case RepostAction.RemoveSurplusBoardSlot:
                await RemoveSurplusBoardSlotIfPresentAsync(staffablePositionCtrlNbr, ct);
                return;

            default:
                return;
        }
    }

    /// <summary>
    /// Removes a vacated extra-board slot that produced no repost bulletin (the board is still
    /// adequately staffed). Such a slot is surplus capacity, so the <see cref="RosterBoardPosition"/>
    /// is detached from its board and its backing <see cref="StaffablePosition"/> is removed. No-op
    /// when the vacated position is not a board slot (e.g. a structural crew position).
    /// </summary>
    private async Task RemoveSurplusBoardSlotIfPresentAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct)
    {
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

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "VacancyRepost: Removed surplus board slot {Position} from board {Board} (board adequately staffed).",
                staffablePositionCtrlNbr.Value, board.CtrlNbr.Value);
        }
    }

    private async Task RemoveSurplusBoardSlotIfPresentAsync(
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        await RemoveSurplusBoardSlotIfPresentAsync(uow, staffablePositionCtrlNbr, ct);
        await uow.CommitAsync(ct);
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
        CancellationToken ct = default,
        bool executeWorkflowTrigger = true,
        bool enforceUnderstaffedPolicy = true)
    {
        await CancelMovesForVacatedTargetAsync(vacatedStaffablePositionCtrlNbr, ct);

        if (executeWorkflowTrigger)
        {
            var workflowExecuted = await TryExecutePositionVacatedWorkflowAsync(
                vacatedStaffablePositionCtrlNbr,
                previousIncumbentCtrlNbr,
                positionTypeOverride: StaffablePositionType.Board,
                boardCtrlNbr: boardCtrlNbr,
                ct: ct);

            if (workflowExecuted)
            {
                await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
                await RemoveSurplusBoardSlotIfNoOpenVacancyAsync(uow, vacatedStaffablePositionCtrlNbr, ct);
                await uow.CommitAsync(ct);
                return;
            }

            return;
        }

        RepostPlan? plan;
        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            if (await HasOpenVacancyAsync(uow, StaffablePositionType.Board, vacatedStaffablePositionCtrlNbr))
                return;

            var board = await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr, ct);
            if (board is null)
                return;

            plan = enforceUnderstaffedPolicy
                ? await BuildBoardRepostPlanAsync(uow, board, vacatedStaffablePositionCtrlNbr, ct)
                : await BuildBoardRepostPlanWithoutUnderstaffedCheckAsync(uow, board, vacatedStaffablePositionCtrlNbr, ct);
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

    private async Task RemoveSurplusBoardSlotIfNoOpenVacancyAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct)
    {
        if (await HasOpenVacancyAsync(uow, StaffablePositionType.Board, staffablePositionCtrlNbr))
            return;

        await RemoveSurplusBoardSlotIfPresentAsync(uow, staffablePositionCtrlNbr, ct);
    }

    private async Task RemoveSurplusBoardSlotIfNoOpenVacancyAsync(
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        await RemoveSurplusBoardSlotIfNoOpenVacancyAsync(uow, staffablePositionCtrlNbr, ct);
        await uow.CommitAsync(ct);
    }

    private async Task<RepostPlan?> BuildBoardRepostPlanWithoutUnderstaffedCheckAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Boards.RosterBoard board,
        ControlNumber vacatedStaffablePositionCtrlNbr,
        CancellationToken ct)
    {
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

    private async Task<bool> TryExecutePositionVacatedWorkflowAsync(
        ControlNumber staffablePositionCtrlNbr,
        ControlNumber? previousIncumbentCtrlNbr,
        string? positionTypeOverride = null,
        ControlNumber? boardCtrlNbr = null,
        CancellationToken ct = default)
    {
        if (previousIncumbentCtrlNbr is null)
            return false;

        var workflowRuntimeService = serviceProvider?.GetService<WorkflowRuntimeService>();
        if (workflowRuntimeService is null)
            return false;

        var payloadEnvelope = await BuildPositionVacatedWorkflowPayloadAsync(
            staffablePositionCtrlNbr,
            previousIncumbentCtrlNbr,
            positionTypeOverride,
            boardCtrlNbr,
            ct);
        if (payloadEnvelope is null)
            return false;

        return await workflowRuntimeService.ExecutePositionVacatedAsync(
            payloadEnvelope.RailroadCtrlNbr,
            payloadEnvelope.Payload,
            correlationId: null,
            ct);
    }

    private async Task<PositionVacatedWorkflowPayloadEnvelope?> BuildPositionVacatedWorkflowPayloadAsync(
        ControlNumber staffablePositionCtrlNbr,
        ControlNumber previousIncumbentCtrlNbr,
        string? positionTypeOverride,
        ControlNumber? boardCtrlNbr,
        CancellationToken ct)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        string? positionType = positionTypeOverride;
        ControlNumber? craftCtrlNbr = null;
        ControlNumber? rosterCtrlNbr = null;

        if (string.Equals(positionType, StaffablePositionType.Board, StringComparison.Ordinal)
            && boardCtrlNbr is not null)
        {
            var board = await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr, ct);
            craftCtrlNbr = board?.CraftCtrlNbr;
            rosterCtrlNbr = board?.RosterCtrlNbr;
        }

        if (craftCtrlNbr is null)
        {
            var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(staffablePositionCtrlNbr);
            if (crewPosition is not null)
            {
                positionType = StaffablePositionType.Crew;
                var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
                craftCtrlNbr = craftRole?.CraftCtrlNbr;
            }
        }

        if (craftCtrlNbr is null)
        {
            var board = await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(staffablePositionCtrlNbr, ct);
            if (board is not null)
            {
                positionType = StaffablePositionType.Board;
                craftCtrlNbr = board.CraftCtrlNbr;
                rosterCtrlNbr = board.RosterCtrlNbr;
            }
        }

        if (craftCtrlNbr is null || string.IsNullOrWhiteSpace(positionType))
            return null;

        if (rosterCtrlNbr is null)
        {
            rosterCtrlNbr = await ResolvePositionVacatedRosterCtrlNbrAsync(
                uow,
                previousIncumbentCtrlNbr,
                craftCtrlNbr,
                ct);
        }

        var craft = await uow.Crafts.GetByCtrlNbrAsync(craftCtrlNbr, ct);
        if (craft is null)
            return null;

        var existingVacancies = await uow.PositionVacancies.GetByTargetAsync(positionType, staffablePositionCtrlNbr);
        var vacancyReasonCode = existingVacancies
            .Where(v => v.Status is "Open" or "Bulletined")
            .OrderByDescending(v => v.CtrlNbr.Value)
            .Select(v => v.VacancyReasonCode)
            .FirstOrDefault() ?? IncumbentRemovedReason;

        var payload = new WorkflowPositionVacatedRuntimePayload(
            StaffablePositionCtrlNbr: staffablePositionCtrlNbr,
            CraftCtrlNbr: craftCtrlNbr,
            PositionTypeCode: positionType,
            VacancyReasonCode: vacancyReasonCode,
            PreviousIncumbentCtrlNbr: previousIncumbentCtrlNbr,
            BoardCtrlNbr: boardCtrlNbr,
            RosterCtrlNbr: rosterCtrlNbr);

        if (craft.DynamicGroupCtrlNbr is null)
            return null;

        return new PositionVacatedWorkflowPayloadEnvelope(craft.DynamicGroupCtrlNbr, payload);
    }

    private static async Task<ControlNumber?> ResolvePositionVacatedRosterCtrlNbrAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        ControlNumber craftCtrlNbr,
        CancellationToken ct)
    {
        var seniorityRows = await uow.Seniority.GetByEmployeeCtrlNbrAsync(employeeCtrlNbr);
        if (seniorityRows.Count == 0)
            return null;

        var rosterCtrlNbrs = seniorityRows
            .Select(s => s.RosterCtrlNbr)
            .Distinct()
            .ToList();
        if (rosterCtrlNbrs.Count == 0)
            return null;

        var rosters = await uow.Rosters.GetByCtrlNbrsAsync(rosterCtrlNbrs, ct);
        var matchingRosterCtrlNbrs = rosters
            .Where(r => r.CraftCtrlNbr == craftCtrlNbr)
            .Select(r => r.CtrlNbr)
            .ToHashSet();
        if (matchingRosterCtrlNbrs.Count == 0)
            return null;

        var selectedSeniority = seniorityRows
            .Where(s => matchingRosterCtrlNbrs.Contains(s.RosterCtrlNbr))
            .OrderByDescending(s => s.LastActiveRoster)
            .ThenByDescending(s => s.RosterDate)
            .FirstOrDefault();

        return selectedSeniority?.RosterCtrlNbr;
    }

    private async Task<RepostDecision> ResolveRepostDecisionAsync(
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var activeAssignment = await uow.PositionAssignments.GetByStaffablePositionAsync(staffablePositionCtrlNbr);
        if (activeAssignment is not null)
            return new RepostDecision(RepostAction.NoAction);

        var crewPosition = await uow.CrewPositions.GetByStaffablePositionAsync(staffablePositionCtrlNbr);
        if (crewPosition is not null)
            return await ResolveCrewRepostDecisionAsync(uow, crewPosition, staffablePositionCtrlNbr, ct);

        var board = await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(staffablePositionCtrlNbr, ct);
        if (board is not null)
            return await ResolveBoardRepostDecisionAsync(uow, board, staffablePositionCtrlNbr, ct);

        return new RepostDecision(RepostAction.NoAction);
    }

    private async Task<RepostDecision> ResolveCrewRepostDecisionAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Crews.CrewPosition crewPosition,
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct)
    {
        var existingVacancies = await uow.PositionVacancies.GetByTargetAsync(StaffablePositionType.Crew, staffablePositionCtrlNbr);
        var existingBulletinedVacancy = existingVacancies.FirstOrDefault(v => v.Status == "Bulletined");
        if (existingBulletinedVacancy is not null)
            return new RepostDecision(RepostAction.NoAction);

        var openIncumbentRemovedVacancy = existingVacancies
            .FirstOrDefault(v => v.Status == "Open" && v.VacancyReasonCode == IncumbentRemovedReason);
        if (openIncumbentRemovedVacancy is not null)
        {
            if (!await HasBulletinRuleAsync(uow, openIncumbentRemovedVacancy.CraftCtrlNbr))
                return new RepostDecision(RepostAction.NoAction);

            return new RepostDecision(
                RepostAction.PostExistingOpenVacancy,
                ExistingVacancyCtrlNbr: openIncumbentRemovedVacancy.CtrlNbr);
        }

        var existingOpenVacancy = existingVacancies.FirstOrDefault(v => v.Status == "Open");
        if (existingOpenVacancy is not null)
            return new RepostDecision(RepostAction.NoAction);

        var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
        var crew = await uow.Crews.GetByCtrlNbrAsync(crewPosition.CrewCtrlNbr, ct);
        if (craftRole is null || crew is null)
        {
            logger.LogWarning(
                "VacancyRepost: Crew position {Position} missing craft role or crew — cannot repost.",
                staffablePositionCtrlNbr.Value);
            return new RepostDecision(RepostAction.NoAction);
        }

        if (!await HasBulletinRuleAsync(uow, craftRole.CraftCtrlNbr))
            return new RepostDecision(RepostAction.NoAction);

        return new RepostDecision(
            RepostAction.OpenVacancyAndBulletin,
            Plan: new RepostPlan(
                WorkAreaGroupCtrlNbr: crew.WorkAreaCtrlNbr,
                TargetType: StaffablePositionType.Crew,
                CraftCtrlNbr: craftRole.CraftCtrlNbr,
                VacancyReasonCode: CrewVacatedReason,
                TargetName: VacancyTargetName.ForCrewPosition(crew, craftRole)));
    }

    private async Task<RepostDecision> ResolveBoardRepostDecisionAsync(
        IOrchestrationUnitOfWork uow,
        Domain.Modules.Boards.RosterBoard board,
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct)
    {
        if (await HasOpenVacancyAsync(uow, StaffablePositionType.Board, staffablePositionCtrlNbr))
            return new RepostDecision(RepostAction.NoAction);

        var repostPlan = await BuildBoardRepostPlanAsync(uow, board, staffablePositionCtrlNbr, ct);
        if (repostPlan is not null)
            return new RepostDecision(RepostAction.OpenVacancyAndBulletin, Plan: repostPlan);

        return new RepostDecision(RepostAction.RemoveSurplusBoardSlot);
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
                await RepostVacatedPositionAsync(staffablePositionCtrlNbr, ct: ct, executeWorkflowTrigger: false);

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

    private async Task PostExistingOpenVacancyAsync(
        ControlNumber vacancyCtrlNbr,
        ControlNumber staffablePositionCtrlNbr,
        CancellationToken ct)
    {
        DateTime? opensUtc = null;
        DateTime? closesUtc = null;
        DateTime? effectiveUtc = null;

        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            var vacancy = await uow.PositionVacancies.GetByCtrlNbrAsync(vacancyCtrlNbr, ct);
            if (vacancy is null
                || vacancy.Status != "Open"
                || vacancy.VacancyReasonCode != IncumbentRemovedReason)
            {
                return;
            }

            var rule = await uow.BulletinRules.GetByCraftAsync(vacancy.CraftCtrlNbr);
            if (rule is null)
            {
                logger.LogWarning(
                    "VacancyRepost: No BulletinRule configured for craft {Craft} — cannot bulletin existing incumbent-removed vacancy {Vacancy}.",
                    vacancy.CraftCtrlNbr.Value,
                    vacancy.CtrlNbr.Value);
                return;
            }

            var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(vacancy.WorkAreaGroupCtrlNbr, ct);
            var tz = ResolveTimeZone(workArea?.TimeZoneId);
            var postingWindow = rule.CalculateBidWindow(DateTime.UtcNow, tz);
            opensUtc = postingWindow.Opens;
            closesUtc = postingWindow.Closes;
            effectiveUtc = postingWindow.Effective;
        }

        if (!opensUtc.HasValue || !closesUtc.HasValue || !effectiveUtc.HasValue)
            return;

        await bulletinsService.PostBulletinForVacancyAsync(
            vacancyCtrlNbr,
            opensUtc.Value,
            closesUtc.Value,
            effectiveUtc.Value,
            ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "VacancyRepost: Posted bulletin for existing incumbent-removed vacancy {Vacancy} on crew position {Position}.",
                vacancyCtrlNbr.Value,
                staffablePositionCtrlNbr.Value);
        }
    }

    private static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return null;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
    }

    private async Task CancelMovesForVacatedTargetAsync(ControlNumber targetPositionCtrlNbr, CancellationToken ct)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var activeMoves = await uow.SeniorityMoves.GetActiveAsync(ct);
        var staleMoves = activeMoves
            .Where(move => move.TargetPositionCtrlNbr == targetPositionCtrlNbr
                           && (move.MoveType == SeniorityMoveType.Voluntary || move.MoveType == SeniorityMoveType.Hangout))
            .ToList();

        if (staleMoves.Count == 0)
            return;

        foreach (var move in staleMoves)
        {
            move.Cancel(VacatedTargetCancellationReason);
            await uow.SeniorityMoves.UpdateAsync(move, ct);
        }

        await uow.CommitAsync(ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "VacancyRepost: Cancelled {Count} stale seniority move(s) targeting vacated position {Position}.",
                staleMoves.Count,
                targetPositionCtrlNbr.Value);
        }
    }

    private sealed record RepostPlan(
        ControlNumber WorkAreaGroupCtrlNbr,
        string TargetType,
        ControlNumber CraftCtrlNbr,
        string VacancyReasonCode,
        string TargetName);

    private enum RepostAction
    {
        NoAction,
        OpenVacancyAndBulletin,
        PostExistingOpenVacancy,
        RemoveSurplusBoardSlot
    }

    private sealed record RepostDecision(
        RepostAction Action,
        RepostPlan? Plan = null,
        ControlNumber? ExistingVacancyCtrlNbr = null);

    private sealed record PositionVacatedWorkflowPayloadEnvelope(
        ControlNumber RailroadCtrlNbr,
        WorkflowPositionVacatedRuntimePayload Payload);
}
