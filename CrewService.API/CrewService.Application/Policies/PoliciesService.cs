using CrewService.Application.BackgroundWorkers;
using CrewService.Application.Notifications;
using CrewService.Application.Staffing;
using CrewService.Application.Time;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Crews;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using System.Linq;

namespace CrewService.Application.Policies;

public sealed class PoliciesService(IOrchestrationUnitOfWorkFactory uowFactory, ISeniorityMoveSignal seniorityMoveSignal, IWorkAreaClock workAreaClock, EmployeeNotificationService notifications)
{
    public async Task<CraftDisplacementPolicy> GetOrUpsertDisplacementPolicyAsync(
        long craftCtrlNbr, int windowHours, string seniorityBasis, string defaultAction,
        string? eligibilitySelectorJson, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.CraftDisplacementPolicies.GetByCraftAsync(ControlNumber.Create(craftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(windowHours, seniorityBasis, defaultAction, eligibilitySelectorJson);
            await uow.CraftDisplacementPolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var policy = CraftDisplacementPolicy.Create(craftCtrlNbr, windowHours, seniorityBasis, defaultAction, eligibilitySelectorJson);
        await uow.CraftDisplacementPolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<CraftDisplacementPolicy> GetDisplacementPolicyAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CraftDisplacementPolicies.GetByCraftAsync(craftCtrlNbr)
            ?? throw new KeyNotFoundException($"Displacement policy for craft {craftCtrlNbr} not found.");
    }

    public async Task<BulletinPolicy> GetOrUpsertBulletinPolicyAsync(
        long craftCtrlNbr, int bidWindowHours, bool forcedAssignmentEnabled, string forcedAssignmentBasis,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.BulletinPolicies.GetByCraftAsync(ControlNumber.Create(craftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(bidWindowHours, forcedAssignmentEnabled, forcedAssignmentBasis);
            await uow.BulletinPolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var policy = BulletinPolicy.Create(craftCtrlNbr, bidWindowHours, forcedAssignmentEnabled, forcedAssignmentBasis);
        await uow.BulletinPolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<BulletinPolicy> GetBulletinPolicyAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.BulletinPolicies.GetByCraftAsync(craftCtrlNbr)
            ?? throw new KeyNotFoundException($"Bulletin policy for craft {craftCtrlNbr} not found.");
    }

    public async Task<SeniorityMovePolicy> GetOrUpsertSeniorityMovePolicyAsync(
        long railroadCtrlNbr, long craftCtrlNbr, int eligibilityDays, int requestHours, int cancelHours, bool autoApprove,
        string crewToCrewStrategy, string crewToBoardStrategy,
        string extraBoardToCrewStrategy, string hangoutToCrewStrategy,
        string extendedAbsenceToCrewStrategy, string trainingToCrewStrategy,
        string newHireToCrewStrategy, bool willWorkEnabled = false,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(ControlNumber.Create(railroadCtrlNbr), ControlNumber.Create(craftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(eligibilityDays, requestHours, cancelHours, autoApprove,
                crewToCrewStrategy, crewToBoardStrategy, extraBoardToCrewStrategy,
                hangoutToCrewStrategy, extendedAbsenceToCrewStrategy, trainingToCrewStrategy, newHireToCrewStrategy,
                willWorkEnabled);
            await uow.SeniorityMovePolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var policy = SeniorityMovePolicy.Create(ControlNumber.Create(railroadCtrlNbr), ControlNumber.Create(craftCtrlNbr),
            eligibilityDays, requestHours, cancelHours, autoApprove,
            crewToCrewStrategy, crewToBoardStrategy, extraBoardToCrewStrategy,
            hangoutToCrewStrategy, extendedAbsenceToCrewStrategy, trainingToCrewStrategy, newHireToCrewStrategy,
            willWorkEnabled);
        await uow.SeniorityMovePolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<SeniorityMovePolicy> GetSeniorityMovePolicyAsync(
        ControlNumber railroadCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(railroadCtrlNbr, craftCtrlNbr)
            ?? throw new KeyNotFoundException($"Seniority move policy for railroad {railroadCtrlNbr} / craft {craftCtrlNbr} not found.");
    }

    public async Task<SeniorityMove> ExerciseSeniorityMoveAsync(
        long railroadCtrlNbr, long employeeCtrlNbr, long craftCtrlNbr, long targetPositionCtrlNbr,
        long? displacedEmployeeCtrlNbr, int daysOnCurrentPosition,
        string moveType = SeniorityMoveType.Voluntary,
        long targetBoardCtrlNbr = 0,
        bool? willWork = null,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var empCtrlNbr = ControlNumber.Create(employeeCtrlNbr);
        var policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(ControlNumber.Create(railroadCtrlNbr), ControlNumber.Create(craftCtrlNbr));

        // Compute days on current position server-side from the live PositionAssignment.
        // The client-supplied value is used as a fallback when no assignment record exists.
        var currentAssignments = await uow.PositionAssignments.GetByEmployeeAsync(empCtrlNbr);
        var earliestAssignment = currentAssignments
            .OrderBy(a => a.AssignedDateUtc)
            .FirstOrDefault();
        if (earliestAssignment is not null)
            daysOnCurrentPosition = (int)(workAreaClock.UtcNow.UtcDateTime - earliestAssignment.AssignedDateUtc).TotalDays;

        // No Access is an administrative forced bump that bypasses the eligibility threshold.
        var isNoAccess = moveType == SeniorityMoveType.NoAccess;

        if (!isNoAccess && policy is not null && daysOnCurrentPosition < policy.EligibilityDays)
            throw new InvalidOperationException(
                $"Employee has only {daysOnCurrentPosition} days on current position; eligibility requires {policy.EligibilityDays}.");

        // Compute effective date. No Access uses a fixed next-day floor (legacy SA rule:
        // DateTime.Today.AddDays(1).AddMinutes(1)); all other moves use the policy-driven strategy.
        var effectiveUtc = isNoAccess
            ? workAreaClock.UtcNow.UtcDateTime.Date.AddDays(1).AddMinutes(1)
            : (await ComputeEffectiveDateAsync(
                empCtrlNbr, ControlNumber.Create(craftCtrlNbr),
                targetBoardCtrlNbr, targetPositionCtrlNbr, earliestAssignment, daysOnCurrentPosition,
                policy, uow, ct)).UtcDateTime;

        // Board join path: create a new position at the bottom of the target board.
        if (targetBoardCtrlNbr > 0)
        {
            var board = await uow.RosterBoards.GetByCtrlNbrAsync(ControlNumber.Create(targetBoardCtrlNbr), ct)
                ?? throw new KeyNotFoundException($"Roster board {targetBoardCtrlNbr} not found.");

            if (!board.AllowSeniorityMove)
                throw new InvalidOperationException($"Board '{board.Name}' does not allow seniority moves.");

            var nextOrder = board.Positions.Count > 0
                ? board.Positions.Max(p => p.PositionOrder) + 1
                : 1;

            var staffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
            var boardPosition = board.AddPosition(empCtrlNbr, nextOrder, staffablePosition.CtrlNbr);
            var positionAssignment = PositionAssignment.Create(
                staffablePosition.CtrlNbr, empCtrlNbr, PositionAssignmentType.Board, boardPosition.CtrlNbr);

            uow.StaffablePositions.Add(staffablePosition);
            uow.PositionAssignments.Add(positionAssignment);
            uow.RosterBoards.Update(board);

            targetPositionCtrlNbr = staffablePosition.CtrlNbr.Value;
            displacedEmployeeCtrlNbr = null;
        }

        // Bump path: if targetPositionCtrlNbr was not supplied, resolve it from the displaced employee's current assignment.
        if (targetPositionCtrlNbr == 0 && displacedEmployeeCtrlNbr is > 0)
        {
            var displacedAssignments = await uow.PositionAssignments.GetByEmployeeAsync(ControlNumber.Create(displacedEmployeeCtrlNbr.Value));
            var displacedAssignment = displacedAssignments.FirstOrDefault()
                ?? throw new InvalidOperationException($"Displaced employee {displacedEmployeeCtrlNbr} has no current position assignment.");
            targetPositionCtrlNbr = displacedAssignment.StaffablePositionCtrlNbr.Value;
        }

        if (targetPositionCtrlNbr == 0)
            throw new InvalidOperationException("A target position or target board must be specified for a seniority move.");

        // Will-work election is only honored when the governing policy enables it.
        // Otherwise no election is recorded (null), matching the legacy "option not offered" case.
        var willWorkElection = policy?.WillWorkEnabled == true ? willWork : null;

        var move = SeniorityMove.Create(ControlNumber.Create(railroadCtrlNbr), ControlNumber.Create(employeeCtrlNbr), ControlNumber.Create(craftCtrlNbr),
            ControlNumber.Create(targetPositionCtrlNbr),
            displacedEmployeeCtrlNbr is null or 0 ? null : ControlNumber.Create(displacedEmployeeCtrlNbr.Value),
            daysOnCurrentPosition, moveType, effectiveUtc, willWorkElection);
        await uow.SeniorityMoves.AddAsync(move, ct);

        // Notify the soon-to-be-displaced employee at request time (position-affecting; requires
        // acknowledgement). Mirrors the legacy SeniorityMoveNotification raised on creation.
        await notifications.NotifySeniorityMoveRequestedAsync(uow, move, ct);

        await uow.CommitAsync(ct);
        seniorityMoveSignal.Notify(move.EffectiveUtc ?? workAreaClock.UtcNow.UtcDateTime);
        return move;
    }

    public async Task<SeniorityMove> ApproveSeniorityMoveAsync(
        ControlNumber moveCtrlNbr, DateTime? effectiveUtc = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var move = await uow.SeniorityMoves.GetByCtrlNbrAsync(moveCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority move {moveCtrlNbr} not found.");
        move.Approve(effectiveUtc);
        await uow.SeniorityMoves.UpdateAsync(move, ct);
        await uow.CommitAsync(ct);
        // Wake the worker at the move's effective time (or immediately if none set)
        seniorityMoveSignal.Notify(move.EffectiveUtc ?? workAreaClock.UtcNow.UtcDateTime);
        return move;
    }

    public async Task<SeniorityMove> RejectSeniorityMoveAsync(
        ControlNumber moveCtrlNbr, string reason, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var move = await uow.SeniorityMoves.GetByCtrlNbrAsync(moveCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority move {moveCtrlNbr} not found.");
        move.Reject(reason);
        await uow.SeniorityMoves.UpdateAsync(move, ct);
        await uow.CommitAsync(ct);
        return move;
    }

    public async Task<SeniorityMove> CancelSeniorityMoveAsync(
        ControlNumber moveCtrlNbr, string reason, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var move = await uow.SeniorityMoves.GetByCtrlNbrAsync(moveCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority move {moveCtrlNbr} not found.");

        // Enforce CancelHours: cannot cancel if within the cancel window before effective time.
        if (move.Status == SeniorityMoveStatus.Approved && move.EffectiveUtc.HasValue)
        {
            var policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(move.RailroadCtrlNbr, move.CraftCtrlNbr);
            if (policy is not null && policy.CancelHours > 0)
            {
                var cancelDeadline = move.EffectiveUtc.Value.AddHours(-policy.CancelHours);
                if (workAreaClock.UtcNow.UtcDateTime > cancelDeadline)
                    throw new InvalidOperationException(
                        $"Cannot cancel: within the {policy.CancelHours}-hour cancel window before effective time {move.EffectiveUtc.Value:u}.");
            }
        }

        move.Cancel(reason);
        await uow.SeniorityMoves.UpdateAsync(move, ct);

        // Notify the previously-bumped employee that the move is off, and clear the stale bump notice.
        await notifications.NotifySeniorityMoveCancelledAsync(uow, move, ct);

        await uow.CommitAsync(ct);
        return move;
    }

    public async Task<SeniorityMove> CompleteSeniorityMoveAsync(
        ControlNumber moveCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var move = await uow.SeniorityMoves.GetByCtrlNbrAsync(moveCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Seniority move {moveCtrlNbr} not found.");
        move.Complete();
        await uow.SeniorityMoves.UpdateAsync(move, ct);
        await uow.CommitAsync(ct);
        return move;
    }

    public async Task<IReadOnlyList<SeniorityMoveListItem>> GetSeniorityMovesByEmployeeAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var moves = await uow.SeniorityMoves.GetByEmployeeAsync(employeeCtrlNbr, ct);
        return await EnrichWithAutoApproveAsync(moves, uow, ct);
    }

    public async Task<IReadOnlyList<SeniorityMoveListItem>> GetSeniorityMovesByCraftAsync(
        ControlNumber craftCtrlNbr, string? status = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var moves = status is not null
            ? await uow.SeniorityMoves.GetByCraftByStatusAsync(craftCtrlNbr, status, ct)
            : await uow.SeniorityMoves.GetByCraftAsync(craftCtrlNbr, ct);
        return await EnrichWithAutoApproveAsync(moves, uow, ct);
    }

    public async Task<IReadOnlyList<SeniorityMoveListItem>> GetPendingSeniorityMovesAsync(
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var moves = await uow.SeniorityMoves.GetPendingAsync(ct);
        return await EnrichWithAutoApproveAsync(moves, uow, ct);
    }

    public async Task<IReadOnlyList<SeniorityMoveListItem>> GetActiveSeniorityMovesAsync(
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var moves = await uow.SeniorityMoves.GetActiveAsync(ct);
        return await EnrichWithAutoApproveAsync(moves, uow, ct);
    }

    public async Task<IReadOnlyList<SeniorityMoveListItem>> GetAllSeniorityMovesAsync(
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var moves = await uow.SeniorityMoves.GetAllMovesAsync(ct);
        return await EnrichWithAutoApproveAsync(moves, uow, ct);
    }

    /// <summary>
    /// Pairs each move with its computed auto-approve flag, mirroring the
    /// <c>SeniorityMoveWorker</c> predicate: a move auto-approves when it is a
    /// NoAccess bump, or when its craft policy exists and has AutoApprove enabled.
    /// Policies are looked up once per craft. Each move is also paired with its
    /// resolved target-position display name and the work-area timezone id of the
    /// target position, both cached once per position.
    /// </summary>
    private async Task<IReadOnlyList<SeniorityMoveListItem>> EnrichWithAutoApproveAsync(
        List<SeniorityMove> moves, IOrchestrationUnitOfWork uow, CancellationToken ct)
    {
        var policyCache = new Dictionary<ControlNumber, SeniorityMovePolicy?>();
        var targetNameCache = new Dictionary<ControlNumber, string>();
        var timeZoneIdCache = new Dictionary<ControlNumber, string?>();
        var items = new List<SeniorityMoveListItem>(moves.Count);
        foreach (var move in moves)
        {
            bool autoApprove;
            if (move.MoveType == SeniorityMoveType.NoAccess)
            {
                autoApprove = true;
            }
            else
            {
                if (!policyCache.TryGetValue(move.CraftCtrlNbr, out var policy))
                {
                    policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(move.RailroadCtrlNbr, move.CraftCtrlNbr);
                    policyCache[move.CraftCtrlNbr] = policy;
                }
                autoApprove = policy is not null && policy.AutoApprove;
            }

            if (!targetNameCache.TryGetValue(move.TargetPositionCtrlNbr, out var targetName))
            {
                targetName = await StaffablePositionNameResolver.ResolveAsync(uow, move.TargetPositionCtrlNbr, ct);
                targetNameCache[move.TargetPositionCtrlNbr] = targetName;
            }

            // Resolve the work-area timezone of the target position so the UI can
            // display the move's UTC instants as work-area-local wall-clock times.
            // Board positions have no crew position, so the zone stays null (UTC).
            if (!timeZoneIdCache.TryGetValue(move.TargetPositionCtrlNbr, out var timeZoneId))
            {
                var crewPos = await uow.CrewPositions.GetByStaffablePositionAsync(move.TargetPositionCtrlNbr);
                var tz = crewPos is not null
                    ? await workAreaClock.GetCrewTimeZoneAsync(uow, crewPos.CrewCtrlNbr, ct)
                    : null;
                timeZoneId = tz?.Id;
                timeZoneIdCache[move.TargetPositionCtrlNbr] = timeZoneId;
            }

            items.Add(new SeniorityMoveListItem(move, autoApprove, targetName, timeZoneId));
        }
        return items;
    }

    public async Task<IReadOnlyList<SeniorityMove>> GetApprovedDueSeniorityMovesAsync(
        DateTime asOf, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityMoves.GetApprovedDueAsync(asOf, ct);
    }

    public async Task<DateTime?> GetNextApprovedSeniorityMoveEffectiveUtcAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityMoves.GetNextApprovedEffectiveUtcAsync(ct);
    }

    public async Task<DateTime?> GetNextActiveSeniorityMoveEffectiveUtcForRailroadAsync(
        ControlNumber railroadCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var active = await uow.SeniorityMoves.GetActiveAsync(ct);
        var nowUtc = workAreaClock.UtcNow.UtcDateTime;
        return active
            .Where(m => m.RailroadCtrlNbr == railroadCtrlNbr
                        && m.EffectiveUtc.HasValue
                        && m.EffectiveUtc.Value >= nowUtc)
            .OrderBy(m => m.EffectiveUtc)
            .Select(m => (DateTime?)m.EffectiveUtc!.Value)
            .FirstOrDefault();
    }

    /// <summary>
    /// Auto-approves all Pending seniority moves whose craft policy has <c>AutoApprove = true</c>.
    /// Called by <c>SeniorityMoveWorker</c>.
    /// </summary>
    public async Task<IReadOnlyList<SeniorityMove>> AutoApprovePendingMovesAsync(CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var pending = await uow.SeniorityMoves.GetPendingAsync(ct);
        var approved = new List<SeniorityMove>();

        foreach (var move in pending)
        {
            // No Access is an administrative forced bump: it always auto-approves,
            // regardless of whether a policy exists or has AutoApprove enabled.
            if (move.MoveType != SeniorityMoveType.NoAccess)
            {
                var policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(move.RailroadCtrlNbr, move.CraftCtrlNbr);
                if (policy is null || !policy.AutoApprove) continue;
            }
            move.Approve();
            await uow.SeniorityMoves.UpdateAsync(move, ct);
            approved.Add(move);
        }

        if (approved.Count > 0)
        {
            await uow.CommitAsync(ct);
            // Notify the signal for the earliest approved effective time
            var earliest = approved
                .Where(m => m.EffectiveUtc.HasValue)
                .OrderBy(m => m.EffectiveUtc)
                .FirstOrDefault();
            if (earliest?.EffectiveUtc is not null)
                seniorityMoveSignal.Notify(earliest.EffectiveUtc.Value);
            else if (approved.Count > 0)
                seniorityMoveSignal.Notify(workAreaClock.UtcNow.UtcDateTime);
        }
        return approved;
    }

    /// <summary>
    /// Returns the computed effective date for a prospective seniority move without persisting anything.
    /// Used by the UI to display the effective date to the employee before they submit.
    /// </summary>
    public async Task<DateTimeOffset> PreviewEffectiveDateAsync(
        long railroadCtrlNbr, long employeeCtrlNbr, long craftCtrlNbr,
        long targetPositionCtrlNbr = 0, long targetBoardCtrlNbr = 0,
        CancellationToken ct = default)
    {
        var (effectiveUtc, _) = await PreviewEffectiveDateWithWillWorkAsync(
            railroadCtrlNbr, employeeCtrlNbr, craftCtrlNbr, targetPositionCtrlNbr, targetBoardCtrlNbr, ct);
        return effectiveUtc;
    }

    /// <summary>
    /// Computes the effective date and whether the "will work" election should be offered.
    /// The election is offered only when the governing policy enables it, the employee is on a
    /// crew position (not a board), and the effective time-of-day equals the current crew
    /// position's on-duty time (i.e. the move takes effect at the start of a shift they would
    /// otherwise work). Mirrors SA's <c>SeniorityMove.WillWorkOption</c>.
    /// </summary>
    public async Task<(DateTimeOffset EffectiveUtc, bool WillWorkOffered)> PreviewEffectiveDateWithWillWorkAsync(
        long railroadCtrlNbr, long employeeCtrlNbr, long craftCtrlNbr,
        long targetPositionCtrlNbr = 0, long targetBoardCtrlNbr = 0,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var empCtrlNbr = ControlNumber.Create(employeeCtrlNbr);
        var policy = await uow.SeniorityMovePolicies.GetByRailroadAndCraftAsync(ControlNumber.Create(railroadCtrlNbr), ControlNumber.Create(craftCtrlNbr));

        var currentAssignments = await uow.PositionAssignments.GetByEmployeeAsync(empCtrlNbr);
        var earliestAssignment = currentAssignments.OrderBy(a => a.AssignedDateUtc).FirstOrDefault();

        int daysOnCurrentPosition = earliestAssignment is not null
            ? (int)(workAreaClock.UtcNow.UtcDateTime - earliestAssignment.AssignedDateUtc).TotalDays
            : 0;

        var effectiveUtc = await ComputeEffectiveDateAsync(
            empCtrlNbr, ControlNumber.Create(craftCtrlNbr),
            targetBoardCtrlNbr, targetPositionCtrlNbr, earliestAssignment, daysOnCurrentPosition,
            policy, uow, ct);

        var willWorkOffered = await IsWillWorkOfferedAsync(
            policy, earliestAssignment, effectiveUtc, uow, ct);

        return (effectiveUtc, willWorkOffered);
    }

    /// <summary>
    /// Determines whether the "will work" election is offered for a move with the given effective date.
    /// Legacy rule (SA <c>WillWorkOption</c>): the employee is on a crew position and the effective
    /// time-of-day equals that crew position's on-duty time.
    /// </summary>
    private async Task<bool> IsWillWorkOfferedAsync(
        SeniorityMovePolicy? policy,
        PositionAssignment? currentAssignment,
        DateTimeOffset effectiveUtc,
        IOrchestrationUnitOfWork uow,
        CancellationToken ct)
    {
        if (policy?.WillWorkEnabled != true) return false;
        if (currentAssignment is null) return false;
        // Only crew positions qualify; board members are never offered the election.
        if (currentAssignment.AssignmentType != PositionAssignmentType.Direct ||
            currentAssignment.AssignmentSourceCtrlNbr is null)
            return false;

        var currentCrewPos = await uow.CrewPositions.GetByCtrlNbrAsync(currentAssignment.AssignmentSourceCtrlNbr, ct);
        var (schedule, _) = await ResolveCrewScheduleAsync(currentCrewPos, uow, ct);
        if (schedule is null || currentCrewPos is null) return false;

        // The on-duty time is a work-area-local wall clock; compare it against the effective
        // instant converted into that same zone.
        var tz = await workAreaClock.GetCrewTimeZoneAsync(uow, currentCrewPos.CrewCtrlNbr, ct);
        var localEffective = tz is null
            ? effectiveUtc.UtcDateTime
            : TimeZoneInfo.ConvertTimeFromUtc(effectiveUtc.UtcDateTime, tz);

        return TimeOnly.FromDateTime(localEffective) == schedule.OnDutyTime;
    }

    /// <summary>
    /// Computes the seniority move effective date using policy-driven strategy fields
    /// and legacy SA rules ported to the new schedule model.
    ///
    /// Strategy dispatch (read from SeniorityMovePolicy):
    ///   Immediate        – effective = UtcNow
    ///   RequestLeadTime  – effective = max(UtcNow + RequestHours, BumpDate)  [no schedule]
    ///   FirstOffDay      – end-of-shift on the last work day of the relevant schedule period;
    ///                      rolls +7 days when within RequestHours lead-time window.
    ///                      Board path: uses CURRENT crew schedule (Engineer end-of-week).
    ///                      Crew path:  uses TARGET position's schedule.
    /// </summary>
    private async Task<DateTimeOffset> ComputeEffectiveDateAsync(
        ControlNumber empCtrlNbr,
        ControlNumber craftCtrlNbr,
        long targetBoardCtrlNbr,
        long targetPositionCtrlNbr,
        PositionAssignment? currentAssignment,
        int daysOnCurrentPosition,
        SeniorityMovePolicy? policy,
        IOrchestrationUnitOfWork uow,
        CancellationToken ct)
    {
        // Instant arithmetic below is in UTC. Schedule-derived wall-clock times (FirstOffDay)
        // are interpreted in the relevant work area's timezone and converted to a true UTC
        // instant before returning.
        static DateTimeOffset AsUtc(DateTime dt) => new(DateTime.SpecifyKind(dt, DateTimeKind.Utc));

        var now          = workAreaClock.UtcNow.UtcDateTime;
        int requestHours = policy?.RequestHours   ?? 0;
        int eligDays     = policy?.EligibilityDays ?? 0;

        // BumpDate: earliest date the employee became/becomes eligible.
        var bumpDate = currentAssignment is not null
            ? currentAssignment.AssignedDateUtc.AddDays(eligDays)
            : now;

        // Determine current board type (null if the employee is on a crew position).
        BoardType? currentBoardType = null;
        if (currentAssignment?.AssignmentType == PositionAssignmentType.Board)
        {
            var boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(craftCtrlNbr, ct);
            var boardPosition = boards.SelectMany(b => b.Positions)
                .FirstOrDefault(p => p.EmployeeCtrlNbr == empCtrlNbr);
            if (boardPosition is not null)
            {
                var currentBoard = boards.FirstOrDefault(b => b.CtrlNbr == boardPosition.RosterBoardCtrlNbr);
                currentBoardType = currentBoard?.BoardType;
            }
        }

        // Resolve the strategy for this transition.
        string strategy;
        if (targetBoardCtrlNbr > 0)
        {
            // Moving to a board.
            strategy = policy?.CrewToBoardStrategy ?? string.Empty;
        }
        else
        {
            // Moving to a crew position — pick strategy based on current source.
            strategy = currentBoardType switch
            {
                BoardType.ExtraBoard       => policy?.ExtraBoardToCrewStrategy       ?? string.Empty,
                BoardType.Hangout          => policy?.HangoutToCrewStrategy          ?? string.Empty,
                BoardType.ExtendedAbsence  => policy?.ExtendedAbsenceToCrewStrategy  ?? string.Empty,
                BoardType.Training         => policy?.TrainingToCrewStrategy         ?? string.Empty,
                BoardType.NewHire          => policy?.NewHireToCrewStrategy          ?? string.Empty,
                null                       => policy?.CrewToCrewStrategy             ?? string.Empty,
                _                         => string.Empty
            };
        }

        if (string.IsNullOrEmpty(strategy))
            throw new InvalidOperationException(
                "No effective-date strategy is configured for this transition type. " +
                "Configure the seniority move policy for this railroad and craft.");

        // ── Immediate ──────────────────────────────────────────────────────────
        if (strategy == SeniorityMoveEffectiveDateStrategy.Immediate)
            return AsUtc(now);

        // ── RequestLeadTime ────────────────────────────────────────────────────
        if (strategy == SeniorityMoveEffectiveDateStrategy.RequestLeadTime)
        {
            var baseDate = now.AddHours(requestHours);
            if (bumpDate > baseDate) baseDate = bumpDate;
            // Yardman/Yardmaster: avoid exact midnight (legacy nudge rule).
            var craft     = await uow.Crafts.GetByCtrlNbrAsync(craftCtrlNbr, ct);
            var craftName = craft?.CraftName ?? string.Empty;
            if ((craftName.Contains("Yardman", StringComparison.OrdinalIgnoreCase) ||
                 craftName.Contains("Yardmaster", StringComparison.OrdinalIgnoreCase))
                && baseDate.TimeOfDay == TimeSpan.Zero)
                baseDate = baseDate.AddMinutes(1);
            return AsUtc(baseDate);
        }

        // ── FirstOffDay ────────────────────────────────────────────────────────
        // Resolve the relevant schedule and the crew position it belongs to (used to resolve
        // the work-area timezone for the off-duty wall-clock time).
        AssignmentSchedule? schedule = null;
        CrewPosition? scheduleCrewPos = null;
        int workDaysMask = 0;

        if (targetBoardCtrlNbr > 0)
        {
            // Moving to a board: use the CURRENT crew position's schedule.
            // (Engineer Crew→Board uses current crew end-of-work-week.)
            if (currentAssignment?.AssignmentType == PositionAssignmentType.Direct &&
                currentAssignment.AssignmentSourceCtrlNbr is not null)
            {
                scheduleCrewPos = await uow.CrewPositions.GetByCtrlNbrAsync(
                    currentAssignment.AssignmentSourceCtrlNbr, ct);
                (schedule, workDaysMask) = await ResolveCrewScheduleAsync(scheduleCrewPos, uow, ct);
            }
        }
        else
        {
            // Moving to a crew position: always use the TARGET position's schedule.
            if (targetPositionCtrlNbr > 0)
            {
                scheduleCrewPos = await uow.CrewPositions.GetByStaffablePositionAsync(
                    ControlNumber.Create(targetPositionCtrlNbr));
                (schedule, workDaysMask) = await ResolveCrewScheduleAsync(scheduleCrewPos, uow, ct);
            }
        }

        // Timezone of the work area that owns the resolved schedule. Null = treat as UTC.
        var scheduleTz = scheduleCrewPos is not null
            ? await workAreaClock.GetCrewTimeZoneAsync(uow, scheduleCrewPos.CrewCtrlNbr, ct)
            : null;

        if (targetBoardCtrlNbr > 0)
        {
            // Board move (FirstOffDay): end of current crew's last work day this week.
            var baseDate = now.AddHours(requestHours);
            if (bumpDate > baseDate) baseDate = bumpDate;

            if (schedule is not null)
                return GetNextEndOfWorkWeek(schedule, AsUtc(baseDate), requestHours, scheduleTz, workDaysMask);

            // No schedule: fall back to next Monday (legacy fallback for Engineers).
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)baseDate.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;
            return AsUtc(baseDate.Date.AddDays(daysUntilMonday));
        }
        else
        {
            // Crew bump (FirstOffDay): last work day of target schedule.
            var anchor = daysOnCurrentPosition < eligDays ? bumpDate : now;

            if (schedule is not null)
                return GetNextEndOfWorkWeek(schedule, AsUtc(anchor), requestHours, scheduleTz, workDaysMask);

            // No schedule: fall back to anchor + RequestHours.
            return AsUtc(anchor.AddHours(requestHours));
        }
    }

    /// <summary>
    /// Resolves the AssignmentSchedule and work-days mask that govern a crew's end-of-work-week.
    /// A crew's true work week is the UNION of all its crew assignments' day masks: a regular crew
    /// has a single assignment, while a relief crew covers several assignments on different days
    /// (e.g. RLF-A: Sun on one, Mon/Tue on another, Wed/Thu on a third → Sun–Thu). The end-of-week
    /// day is the latest day in that union, and the governing off-duty time comes from the
    /// assignment that covers that last day. Returns null when the crew position has no schedule.
    /// </summary>
    private static async Task<(AssignmentSchedule? Schedule, int WorkDaysMask)> ResolveCrewScheduleAsync(
        CrewPosition? crewPosition,
        IOrchestrationUnitOfWork uow,
        CancellationToken ct)
    {
        _ = ct;
        if (crewPosition is null) return (null, 0);

        var crewAssignments = await uow.CrewAssignments.GetByCrewAsync(crewPosition.CrewCtrlNbr);
        if (crewAssignments.Count == 0) return (null, 0);

        // Union of every assignment's days = the crew's actual weekly footprint.
        int unionMask = 0;
        foreach (var ca in crewAssignments) unionMask |= ca.DaysOfWeekMask;

        // Last day of the crew's contiguous work block. Uses the rest-gap rule so weeks that wrap
        // the Sat->Sun boundary resolve correctly (e.g. Fri,Sat,Sun,Mon,Tue ends on Tuesday).
        int lastDay = FindLastWorkDayOfWeek(unionMask);

        // The assignment covering that last day supplies the governing schedule (its off-duty
        // wall-clock time is when the crew finishes for the week). Fall back to the assignment with
        // the most days when no union day is set (defensive; shouldn't happen with real data).
        var governingAssignment = lastDay >= 0
            ? crewAssignments.First(ca => (ca.DaysOfWeekMask & (1 << lastDay)) != 0)
            : crewAssignments.OrderByDescending(ca => CountBits(ca.DaysOfWeekMask)).First();

        var schedules = await uow.AssignmentSchedules
            .GetByAssignmentAsync(governingAssignment.AssignmentCtrlNbr);

        // Among that assignment's schedules, pick the shift whose operating days best overlap the
        // crew's days on that assignment (handles multi-shift assignments).
        var schedule = schedules
            .OrderByDescending(s => CountBits(s.OperatingDaysMask & governingAssignment.DaysOfWeekMask))
            .FirstOrDefault();

        return (schedule, governingAssignment.DaysOfWeekMask);
    }
    /// <summary>
    /// Returns the off-duty time of the LAST scheduled work day of the current schedule week
    /// whose off-duty datetime is after <paramref name="baseDate"/>. If that time is before
    /// the RequestHours minimum lead time, advances by 7 days (legacy SA rule).
    /// </summary>
    private DateTimeOffset GetNextEndOfWorkWeek(
        AssignmentSchedule schedule, DateTimeOffset baseDate, int requestHours, TimeZoneInfo? tz, int workDaysMask)
    {
        var minTime = baseDate.AddHours(requestHours);

        // Find the LAST day the crew actually works: the crew's work days narrowed to the
        // schedule's operating days. The assignment may be staffed every day while the crew only
        // covers part of the week (e.g. a relief crew on Wed/Thu), so the crew mask — not the full
        // schedule mask — determines the end-of-work-week day. Fall back to the schedule's
        // operating days when no crew mask is available or the intersection is empty.
        int effectiveMask = workDaysMask != 0 ? schedule.OperatingDaysMask & workDaysMask : schedule.OperatingDaysMask;
        if (effectiveMask == 0) effectiveMask = schedule.OperatingDaysMask;

        // Last day of the contiguous work block (rest-gap rule), so weeks that wrap the Sat->Sun
        // boundary (e.g. Fri,Sat,Sun,Mon,Tue) correctly end on Tuesday rather than Saturday.
        int last = FindLastWorkDayOfWeek(effectiveMask);
        DayOfWeek? lastWorkDay = last >= 0 ? (DayOfWeek)last : null;

        if (lastWorkDay is null) return baseDate;

        // Walk the day-of-week in WORK-AREA-LOCAL time and combine with the local off-duty
        // wall-clock time, then convert to a true UTC instant. Combining a UTC date with a
        // local TimeOnly (the old behavior) produced an instant wrong by the zone offset,
        // which is what shifted displayed effective times (e.g. 7:00 AM → 2:00 AM).
        var localBase = tz is null
            ? baseDate.UtcDateTime
            : TimeZoneInfo.ConvertTimeFromUtc(baseDate.UtcDateTime, tz);

        int daysToAdd = ((int)lastWorkDay.Value - (int)localBase.DayOfWeek + 7) % 7;
        var localDate = DateOnly.FromDateTime(localBase).AddDays(daysToAdd);
        var endOfWeek = workAreaClock.CombineLocalToUtc(localDate, schedule.OffDutyTime, tz);

        if (endOfWeek < minTime)
            endOfWeek = workAreaClock.CombineLocalToUtc(localDate.AddDays(7), schedule.OffDutyTime, tz);

        return endOfWeek;
    }
    /// <summary>Counts the number of set bits (population count) in a bitmask.</summary>
    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0) { count += mask & 1; mask >>= 1; }
        return count;
    }

    /// <summary>
    /// Returns the last day (0=Sun .. 6=Sat) of the crew's primary contiguous work block using the
    /// rest-gap rule: the last work day is one whose following day is a rest day. This resolves
    /// schedules that wrap the Sat->Sun boundary correctly — e.g. Fri,Sat,Sun,Mon,Tue (off Wed,Thu)
    /// ends on Tuesday, not Saturday. When multiple blocks exist, the end of the longest block wins.
    /// Returns -1 when no day is set, and Saturday when every day is worked (no rest gap to anchor on).
    /// </summary>
    private static int FindLastWorkDayOfWeek(int mask)
    {
        mask &= 0x7F;
        if (mask == 0) return -1;
        if (mask == 0x7F) return (int)DayOfWeek.Saturday; // no rest gap; default to calendar week end

        int bestEnd = -1;
        int bestLen = -1;
        for (int d = 0; d < 7; d++)
        {
            bool isWork    = (mask & (1 << d)) != 0;
            bool nextIsRest = (mask & (1 << ((d + 1) % 7))) == 0;
            if (!isWork || !nextIsRest) continue;

            // Measure this block by walking backwards from d until a rest day is hit.
            int len = 0;
            for (int p = d; len <= 7; p--)
            {
                int day = ((p % 7) + 7) % 7;
                if ((mask & (1 << day)) == 0) break;
                len++;
            }
            if (len > bestLen) { bestLen = len; bestEnd = d; }
        }
        return bestEnd;
    }
}