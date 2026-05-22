using CrewService.Application.Bulletins;
using CrewService.Application.RosterBoardOps;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.SeniorityOps;

public sealed class SeniorityStateVacancyConfigService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    BulletinsService bulletinsService,
    RosterBoardAppService rosterBoardAppService,
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
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var roster = await uow.Rosters.GetByCtrlNbrAsync(rosterCtrlNbr, ct);
        if (roster is null)
        {
            logger.LogWarning("ApplyVacancyAction: Roster {Roster} not found — skipping.", rosterCtrlNbr.Value);
            return;
        }

        // Resolve the railroad from the work area's RailroadCtrlNbr
        var workArea = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr);
        if (workArea?.RailroadCtrlNbr is null)
        {
            logger.LogWarning("ApplyVacancyAction: Work area {WorkArea} has no railroad — skipping.", roster.WorkAreaGroupCtrlNbr.Value);
            return;
        }

        var railroadCtrlNbr = workArea.RailroadCtrlNbr;

        var config = await uow.SeniorityStateVacancyConfigs
            .GetBySeniorityStateAsync(railroadCtrlNbr, newSeniorityStateCtrlNbr, ct);

        if (config is null || config.VacancyAction == VacancyAction.None)
            return;

        if (config.VacancyAction == VacancyAction.VacateAndBulletin)
        {
            var assignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
            foreach (var assignment in assignments)
            {
                try
                {
                    await bulletinsService.OpenVacancyAsync(
                        workAreaGroupCtrlNbr: roster.WorkAreaGroupCtrlNbr,
                        targetType: assignment.AssignmentType,
                        targetCtrlNbr: assignment.StaffablePositionCtrlNbr,
                        craftCtrlNbr: roster.CraftCtrlNbr,
                        vacancyReasonCode: "StatusChange",
                        previousIncumbentCtrlNbr: employeeCtrlNbr,
                        ct: ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ApplyVacancyAction: Failed to open vacancy for position {Position}.", assignment.StaffablePositionCtrlNbr.Value);
                }
            }
        }
        else if (config.VacancyAction == VacancyAction.MoveToBoard && config.TargetBoardType is not null)
        {
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

            // Vacate and bulletin all current positions before placing on the new board.
            var assignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
            foreach (var assignment in assignments)
            {
                try
                {
                    await bulletinsService.OpenVacancyAsync(
                        workAreaGroupCtrlNbr: roster.WorkAreaGroupCtrlNbr,
                        targetType: assignment.AssignmentType,
                        targetCtrlNbr: assignment.StaffablePositionCtrlNbr,
                        craftCtrlNbr: roster.CraftCtrlNbr,
                        vacancyReasonCode: "StatusChange",
                        previousIncumbentCtrlNbr: employeeCtrlNbr,
                        ct: ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ApplyVacancyAction (MoveToBoard): Failed to open vacancy for position {Position}.", assignment.StaffablePositionCtrlNbr.Value);
                }
            }

            // Place the employee at the end of the target board.
            var nextOrder = targetBoard.Positions.Count + 1;
            try
            {
                await rosterBoardAppService.AddRosterBoardPositionAsync(targetBoard.CtrlNbr, employeeCtrlNbr, nextOrder, ct);
                logger.LogInformation(
                    "ApplyVacancyAction (MoveToBoard): Employee {Employee} placed on board {Board} ({BoardType}) at position {Order}.",
                    employeeCtrlNbr.Value, targetBoard.CtrlNbr.Value, config.TargetBoardType, nextOrder);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ApplyVacancyAction (MoveToBoard): Failed to place employee {Employee} on board {Board}.",
                    employeeCtrlNbr.Value, targetBoard.CtrlNbr.Value);
            }
        }
    }
}
