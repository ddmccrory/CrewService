using CrewService.Domain.ValueObjects;

namespace CrewService.Application.VacancyAssignment;

/// <summary>
/// Centralized policy for auto-bulletining vacated positions. This is the single source of
/// truth for the rule "any vacated crew position is auto-bulletined; an extra-board position
/// is auto-bulletined only when its occupancy falls below the board's RequiredPositions".
///
/// Every vacate — regardless of source (crew incumbency end, board removal, seniority move,
/// force assign, off-property transition) — routes through this service so a freed position is
/// bulletined by exactly one code path.
/// </summary>
public interface IVacancyRepostService
{
    /// <summary>
    /// Reposts a single vacated staffable position under the standard policy. No-op when the
    /// position is refilled, already bulletined, not bulletinable, or (for a board slot) the
    /// board is still adequately staffed. Callers must have committed the vacate first so the
    /// position reads as open.
    /// </summary>
    Task RepostVacatedPositionAsync(
        ControlNumber staffablePositionCtrlNbr,
        ControlNumber? previousIncumbentCtrlNbr = null,
        CancellationToken ct = default,
        bool executeWorkflowTrigger = true);

    /// <summary>
    /// Reposts a specific extra-board slot when the board's occupancy has fallen below its
    /// RequiredPositions threshold. Used by board-management flows where the position-to-board
    /// link is already severed, so the board must be supplied explicitly.
    /// </summary>
    Task RepostBoardPositionIfUnderstaffedAsync(
        ControlNumber boardCtrlNbr,
        ControlNumber vacatedStaffablePositionCtrlNbr,
        ControlNumber? previousIncumbentCtrlNbr = null,
        CancellationToken ct = default,
        bool executeWorkflowTrigger = true,
        bool enforceUnderstaffedPolicy = true);

    /// <summary>
    /// Durable reconciliation sweep. Finds vacant positions that were not reposted inline
    /// (e.g. the process restarted mid-request) and reposts them. Invoked periodically by the
    /// BulletinProcessingWorker. Idempotent. Returns the number of positions reposted.
    /// </summary>
    Task<int> ReconcileUnbulletinedVacantPositionsAsync(CancellationToken ct = default);
}
