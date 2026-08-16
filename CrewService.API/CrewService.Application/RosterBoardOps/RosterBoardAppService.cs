using CrewService.Application.Boards;
using CrewService.Application.DailyOperations;
using CrewService.Application.Notifications;
using CrewService.Application.Policies;
using CrewService.Application.Qualifications;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.RosterBoardOps;

public sealed class RosterBoardAppService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    RequirementEvaluationService requirementEvaluationService,
    IRequiredPositionsFormulaRegistry formulaRegistry,
    VacancyAssignment.IVacancyRepostService vacancyRepostService,
    EmployeeNotificationService notifications,
    CallSheetVacancyProjectionSyncService vacancyProjectionSyncService,
    IncumbentAssignmentPath? incumbentAssignmentPath = null)
{
    private readonly IncumbentAssignmentPath _incumbentAssignmentPath = incumbentAssignmentPath ?? new(new(), vacancyProjectionSyncService);

    // ── Single Board ─────────────────────────────────────────────────────────

    public async Task<(RosterBoard? Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName, string? WorkAreaTimeZoneId, Dictionary<ControlNumber, List<string>> RestrictionLabels)>
        GetRosterBoardDetailAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(ctrlNbr, ct);
        if (board is null)
            return (null, string.Empty, string.Empty, 0, string.Empty, null, []);

        var craftName = string.Empty;
        var rosterName = string.Empty;
        long workAreaCtrlNbr = 0;
        var workAreaName = string.Empty;
        string? workAreaTimeZoneId = null;

        if (board.CraftCtrlNbr is not null)
        {
            var craft = await uow.Crafts.GetByCtrlNbrAsync(board.CraftCtrlNbr, ct);
            craftName = craft?.CraftName ?? string.Empty;
        }

        if (board.RosterCtrlNbr is not null)
        {
            var roster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr, ct);
            rosterName = roster?.RosterName ?? string.Empty;
            workAreaCtrlNbr = roster?.WorkAreaGroupCtrlNbr.Value ?? 0;
            if (roster?.WorkAreaGroupCtrlNbr is not null)
            {
                var group = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr, ct);
                workAreaName = group?.Name ?? string.Empty;
                workAreaTimeZoneId = group?.TimeZoneId;
            }
        }

        var positionEmps = board.Positions.Select(p => p.EmployeeCtrlNbr).Where(e => e is not null).Distinct().ToList();
        var restrictionLabels = await ComputeRestrictionLabelsAsync(uow, board.CraftCtrlNbr, positionEmps!, ct);
        return (board, craftName, rosterName, workAreaCtrlNbr, workAreaName, workAreaTimeZoneId, restrictionLabels);
    }

    /// <summary>
    /// Resolves the work-area time zone id for a roster board (board → roster → work-area dynamic group),
    /// or null when the board has no roster/work-area so the caller can fall back to UTC display.
    /// </summary>
    public async Task<string?> GetBoardWorkAreaTimeZoneIdAsync(ControlNumber boardCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr, ct);
        if (board?.RosterCtrlNbr is null) return null;
        var roster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr, ct);
        if (roster?.WorkAreaGroupCtrlNbr is null) return null;
        var group = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr, ct);
        return group?.TimeZoneId;
    }

    // ── Board List ───────────────────────────────────────────────────────────

    public sealed record BoardListResult(
        IReadOnlyList<RosterBoard> Boards,
        Dictionary<ControlNumber, string> CraftNames,
        Dictionary<ControlNumber, Roster> RosterMap,
        Dictionary<ControlNumber, string> GroupNames,
        Dictionary<ControlNumber, string?> GroupTimeZones,
        Dictionary<ControlNumber, Employee> EmployeeMap,
        Dictionary<ControlNumber, List<string>> RestrictionLabels);

    public async Task<BoardListResult> GetAllRosterBoardsAsync(
        long craftCtrlNbr, long parentCtrlNbr, long dynamicGroupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        IReadOnlyList<RosterBoard> boards;
        if (craftCtrlNbr > 0)
        {
            boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(ControlNumber.Create(craftCtrlNbr), ct);
        }
        else if (parentCtrlNbr > 0)
        {
            var crafts = await uow.Crafts.GetByParentAndRailroadAsync(
                ControlNumber.Create(parentCtrlNbr),
                dynamicGroupCtrlNbr > 0 ? ControlNumber.Create(dynamicGroupCtrlNbr) : null);
            var craftCtrlNbrs = crafts.Select(c => c.CtrlNbr).ToList();
            boards = craftCtrlNbrs.Count > 0 ? await uow.RosterBoards.GetByCraftCtrlNbrsAsync(craftCtrlNbrs, ct) : [];
        }
        else
        {
            boards = await uow.RosterBoards.GetAllAsync(ct);
        }

        if (boards.Count == 0)
            return new BoardListResult(boards, [], [], [], [], [], []);

        var distinctCraftCtrlNbrs = boards.Select(b => b.CraftCtrlNbr).Where(c => c is not null).Distinct().ToList();
        var allPositionEmployeeCtrlNbrs = boards.SelectMany(b => b.Positions)
            .Select(p => p.EmployeeCtrlNbr).Where(e => e is not null).Distinct().ToList();

        var craftTasks = await Task.WhenAll(distinctCraftCtrlNbrs.Select(c => uow.Crafts.GetByCtrlNbrAsync(c!, ct)));
        var rosters = distinctCraftCtrlNbrs.Count > 0
            ? await uow.Rosters.GetByCraftCtrlNbrsAsync(distinctCraftCtrlNbrs!)
            : [];
        var employees = allPositionEmployeeCtrlNbrs.Count > 0
            ? await uow.Employees.GetByCtrlNbrsAsync(allPositionEmployeeCtrlNbrs!, ct)
            : [];

        var craftMap = craftTasks.Where(c => c is not null).ToDictionary(c => c!.CtrlNbr, c => c!.CraftName);
        var rosterMap = rosters.ToDictionary(r => r.CtrlNbr, r => r);
        var employeeMap = employees.ToDictionary(e => e.CtrlNbr, e => e);

        var distinctWorkAreaCtrlNbrs = rosterMap.Values.Select(r => r.WorkAreaGroupCtrlNbr).Distinct().ToList();
        var groups = distinctWorkAreaCtrlNbrs.Count > 0
            ? await uow.DynamicGroups.GetByCtrlNbrsAsync(distinctWorkAreaCtrlNbrs)
            : [];
        var groupMap = groups.ToDictionary(g => g.CtrlNbr, g => g.Name);
        var groupTimeZones = groups.ToDictionary(g => g.CtrlNbr, g => g.TimeZoneId);

        var restrictionLabels = await ComputeRestrictionLabelsAsync(
            uow, distinctCraftCtrlNbrs!, allPositionEmployeeCtrlNbrs!, ct);

        return new BoardListResult(boards, craftMap, rosterMap, groupMap, groupTimeZones, employeeMap, restrictionLabels);
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<(RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName)>
        CreateRosterBoardAsync(long craftCtrlNbr, long rosterCtrlNbr, string name,
            BoardType boardType, RotationType rotationType, bool isActive, int requiredPositions = 0,
            bool? allowBulletinBidding = null, bool? allowSeniorityMove = null,
            bool? allowForceAssign = null, bool? notifyOnPlacement = null,
            bool? placementRequiresAcknowledgement = null, CancellationToken ct = default)
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(craftCtrlNbr), ControlNumber.Create(rosterCtrlNbr),
            name, boardType, rotationType, isActive, requiredPositions);
        if (allowBulletinBidding.HasValue)
            board.SetAllowBulletinBidding(allowBulletinBidding.Value);
        if (allowSeniorityMove.HasValue)
            board.SetAllowSeniorityMove(allowSeniorityMove.Value);
        if (allowForceAssign.HasValue)
            board.SetAllowForceAssign(allowForceAssign.Value);
        if (notifyOnPlacement.HasValue)
            board.SetNotifyOnPlacement(notifyOnPlacement.Value);
        if (placementRequiresAcknowledgement.HasValue)
            board.SetPlacementRequiresAcknowledgement(placementRequiresAcknowledgement.Value);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.RosterBoards.Add(board);
        var createResult = await ResolveBoardDetailsAsync(uow, board, ct);
        await uow.CommitAsync(ct);

        return createResult;
    }

    public async Task<(RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName)>
        UpdateRosterBoardAsync(ControlNumber ctrlNbr, string name,
            BoardType boardType, RotationType rotationType, bool isActive, int requiredPositions = 0,
            bool? allowBulletinBidding = null, bool? allowSeniorityMove = null,
            bool? allowForceAssign = null, bool? notifyOnPlacement = null,
            bool? placementRequiresAcknowledgement = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Roster board {ctrlNbr.Value} not found.");
        board.Update(name, boardType, rotationType, isActive, requiredPositions);
        if (allowBulletinBidding.HasValue)
            board.SetAllowBulletinBidding(allowBulletinBidding.Value);
        if (allowSeniorityMove.HasValue)
            board.SetAllowSeniorityMove(allowSeniorityMove.Value);
        if (allowForceAssign.HasValue)
            board.SetAllowForceAssign(allowForceAssign.Value);
        if (notifyOnPlacement.HasValue)
            board.SetNotifyOnPlacement(notifyOnPlacement.Value);
        if (placementRequiresAcknowledgement.HasValue)
            board.SetPlacementRequiresAcknowledgement(placementRequiresAcknowledgement.Value);
        uow.RosterBoards.Update(board);
        var updateResult = await ResolveBoardDetailsAsync(uow, board, ct);
        await uow.CommitAsync(ct);
        return updateResult;
    }

    public async Task<ControlNumber?> DeleteRosterBoardAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Roster board {ctrlNbr.Value} not found.");
        uow.RosterBoards.Remove(board);
        await uow.CommitAsync(ct);
        return board.CtrlNbr;
    }

    // ── Positions ────────────────────────────────────────────────────────────

    public async Task<(RosterBoardPosition Position, Dictionary<ControlNumber, List<string>> RestrictionLabels)>
        AddRosterBoardPositionAsync(ControlNumber boardCtrlNbr, ControlNumber employeeCtrlNbr,
            int positionOrder, DateTime? assignedDateUtc = null, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var result = await AddRosterBoardPositionInOrchestrationAsync(
            uow,
            boardCtrlNbr,
            employeeCtrlNbr,
            positionOrder,
            assignedDateUtc,
            ct);
        await uow.CommitAsync(ct);
        return result;
    }

    public async Task<(RosterBoardPosition Position, Dictionary<ControlNumber, List<string>> RestrictionLabels)>
        AddRosterBoardPositionInOrchestrationAsync(
            IOrchestrationUnitOfWork uow,
            ControlNumber boardCtrlNbr,
            ControlNumber employeeCtrlNbr,
            int positionOrder,
            DateTime? assignedDateUtc = null,
            CancellationToken ct = default)
    {
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Roster board {boardCtrlNbr.Value} not found.");

        var existingAssignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        if (existingAssignments.Any(a => !a.IsDeleted))
            throw new InvalidOperationException(
                "This employee is already assigned to a staffable position. Unassign them first.");

        var staffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
        var position = board.AddPosition(employeeCtrlNbr, positionOrder, staffablePosition.CtrlNbr);

        uow.StaffablePositions.Add(staffablePosition);
        await _incumbentAssignmentPath.AssignAsync(
            uow,
            staffablePosition.CtrlNbr,
            employeeCtrlNbr,
            PositionAssignmentType.Board,
            assignmentSourceCtrlNbr: position.CtrlNbr,
            assignedDateUtc: assignedDateUtc,
            cancellationReason: IncumbentAssignmentPath.DefaultCancellationReason,
            excludeMoveCtrlNbr: null,
            ct);

        uow.RosterBoards.Update(board);

        // Board-placement notification (tenant-configured per board): fires here so every manual
        // add and every seniority-state MoveToBoard placement (which routes through this method)
        // honors the board's NotifyOnPlacement / PlacementRequiresAcknowledgement policy. Emitted
        // inside this UoW so the notice is persisted atomically with the placement.
        await notifications.NotifyBoardPlacementAsync(uow, board, employeeCtrlNbr, subject: null, ct);

        var labels = await ComputeRestrictionLabelsAsync(uow, board.CraftCtrlNbr, [employeeCtrlNbr], ct);
        return (position, labels);
    }

    public Task<ControlNumber> RemoveRosterBoardPositionAsync(ControlNumber positionCtrlNbr, CancellationToken ct = default)
        => RemoveRosterBoardPositionAsync(positionCtrlNbr, reassignEmployee: true, ct);

    public async Task<ControlNumber> RemoveRosterBoardPositionAsync(
        ControlNumber positionCtrlNbr,
        bool reassignEmployee,
        CancellationToken ct = default)
    {
        RemoveBoardPositionResult result;
        await using (var uow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            result = await RemoveRosterBoardPositionInOrchestrationAsync(
                uow,
                positionCtrlNbr,
                reassignEmployee,
                ct);
            await uow.CommitAsync(ct);
        }

        // Auto-bulletin the vacated slot via the centralized policy (occupancy < RequiredPositions).
        // The removal is already committed so the occupancy check sees the post-removal state.
        if (result.IsExtraBoard)
        {
            await vacancyRepostService.RepostBoardPositionIfUnderstaffedAsync(
                result.BoardCtrlNbr,
                result.VacatedStaffablePositionCtrlNbr,
                result.PreviousIncumbentCtrlNbr,
                ct);
        }

        return positionCtrlNbr;
    }

    public async Task<RemoveBoardPositionResult> RemoveRosterBoardPositionInOrchestrationAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber positionCtrlNbr,
        bool reassignEmployee,
        CancellationToken ct = default)
    {
        _ = reassignEmployee;
        var boards = await uow.RosterBoards.GetAllAsync(ct);
        var board = boards.FirstOrDefault(b => b.Positions.Any(p => p.CtrlNbr == positionCtrlNbr))
            ?? throw new KeyNotFoundException($"Position {positionCtrlNbr.Value} not found on any board.");

        var position = board.Positions.First(p => p.CtrlNbr == positionCtrlNbr);
        var positionAssignment = await uow.PositionAssignments.GetByStaffablePositionAsync(position.StaffablePositionCtrlNbr);

        var boardCtrlNbr = board.CtrlNbr;
        var vacatedStaffablePositionCtrlNbr = position.StaffablePositionCtrlNbr;
        var previousIncumbentCtrlNbr = position.EmployeeCtrlNbr;
        var isExtraBoard = board.BoardType == BoardType.ExtraBoard && board.CraftCtrlNbr is not null;

        board.RemovePosition(position);
        uow.RosterBoards.Update(board);
        if (positionAssignment is not null)
        {
            uow.PositionAssignments.Remove(positionAssignment);
            await CallSheetIncumbentSyncService.SyncStaffablePositionIncumbentAsync(
                uow,
                position.StaffablePositionCtrlNbr,
                incumbentEmployeeCtrlNbr: null,
                ct);
            await vacancyProjectionSyncService.ReconcileFromStaffablePositionChangeAsync(
                uow,
                position.StaffablePositionCtrlNbr,
                ct);
        }

        // Refresh the required-position threshold using the craft's assigned strategy (ExtraBoard only).
        if (isExtraBoard)
        {
            var recalcRoster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr, ct);
            if (recalcRoster is not null)
                await RecalculateRequiredPositionsAsync(uow, board, recalcRoster.WorkAreaGroupCtrlNbr, ct);
        }

        return new RemoveBoardPositionResult(
            boardCtrlNbr,
            vacatedStaffablePositionCtrlNbr,
            previousIncumbentCtrlNbr,
            isExtraBoard);
    }

    public async Task<(RosterBoardPosition Position, Dictionary<ControlNumber, List<string>> RestrictionLabels)>
        RestorePositionAsync(ControlNumber positionCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var boards = await uow.RosterBoards.GetAllAsync(ct);
        var board = boards.FirstOrDefault(b => b.Positions.Any(p => p.CtrlNbr == positionCtrlNbr))
            ?? throw new KeyNotFoundException($"Position {positionCtrlNbr.Value} not found.");
        var position = board.Positions.First(p => p.CtrlNbr == positionCtrlNbr);
        var shiftInstances = await uow.ShiftInstances.GetAllAsync(ct);
        foreach (var shift in shiftInstances)
        {
            var boardSlot = shift.BoardSlots.FirstOrDefault(s => s.RosterBoardPositionCtrlNbr == positionCtrlNbr);
            if (boardSlot is null)
                continue;

            if (boardSlot.Status == Domain.Modules.WorkManagement.BoardSlotStatus.MarkedOff
                || boardSlot.Status == Domain.Modules.WorkManagement.BoardSlotStatus.Unavailable)
            {
                boardSlot.RestoreToAvailable();
            }
            await uow.ShiftInstances.UpdateAsync(shift, ct);
            break;
        }

        var labels = await ComputeRestrictionLabelsAsync(uow, board.CraftCtrlNbr, [position.EmployeeCtrlNbr], ct);
        await uow.CommitAsync(ct);
        return (position, labels);
    }

    public async Task<(RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName)>
        ReorderRosterBoardPositionsAsync(ControlNumber boardCtrlNbr,
            List<(ControlNumber PositionCtrlNbr, int PositionOrder)> ordering, CancellationToken ct = default)
    {
        (RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName) reorderResult;
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Roster board {boardCtrlNbr.Value} not found.");
        board.ReorderPositions(ordering);
        uow.RosterBoards.Update(board);
        reorderResult = await ResolveBoardDetailsAsync(uow, board, ct);
        await uow.CommitAsync(ct);

        await ReconcileVacancyProjectionsForBoardOrderChangeAsync(boardCtrlNbr, ct);
        return reorderResult;
    }

    public async Task<(RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName)>
        MoveRosterBoardPositionAsync(
            ControlNumber boardCtrlNbr,
            ControlNumber positionCtrlNbr,
            bool moveUp,
            CancellationToken ct = default)
    {
        (RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName) reorderResult;
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Roster board {boardCtrlNbr.Value} not found.");

        var orderedPositions = board.Positions
            .OrderBy(position => position.PositionOrder)
            .ThenBy(position => position.CtrlNbr.Value)
            .ToList();

        var currentIndex = orderedPositions.FindIndex(position => position.CtrlNbr == positionCtrlNbr);
        if (currentIndex < 0)
            throw new KeyNotFoundException($"Position {positionCtrlNbr.Value} not found on roster board {boardCtrlNbr.Value}.");

        var targetIndex = moveUp ? currentIndex - 1 : currentIndex + 1;
        if (targetIndex < 0 || targetIndex >= orderedPositions.Count)
            return await ResolveBoardDetailsAsync(uow, board, ct);

        var ordering = orderedPositions
            .Select((position, index) =>
            {
                var newOrder = index == currentIndex
                    ? targetIndex + 1
                    : index == targetIndex
                        ? currentIndex + 1
                        : index + 1;
                return (PositionCtrlNbr: position.CtrlNbr, PositionOrder: newOrder);
            })
            .ToList();

        board.ReorderPositions(ordering);
        uow.RosterBoards.Update(board);
        reorderResult = await ResolveBoardDetailsAsync(uow, board, ct);
        await uow.CommitAsync(ct);

        await ReconcileVacancyProjectionsForBoardOrderChangeAsync(boardCtrlNbr, ct);
        return reorderResult;
    }

    // ── Position Assignment Lookups ──────────────────────────────────────────

    public async Task<PositionAssignment?> GetPositionAssignmentAsync(
        ControlNumber staffablePositionCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.PositionAssignments.GetByStaffablePositionAsync(staffablePositionCtrlNbr);
    }

    public async Task<PositionAssignment?> GetBoardPositionAssignmentAsync(
        ControlNumber boardPositionCtrlNbr,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var employeeAssignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        return employeeAssignments.FirstOrDefault(a =>
            a.AssignmentType == PositionAssignmentType.Board
            && a.AssignmentSourceCtrlNbr == boardPositionCtrlNbr);
    }

    public async Task<Dictionary<ControlNumber, PositionAssignment>> GetPositionAssignmentsBatchAsync(
        IEnumerable<ControlNumber?> staffablePositionCtrlNbrs, CancellationToken ct = default)
    {
        var ctrlNbrs = staffablePositionCtrlNbrs.Where(c => c is not null).Distinct().ToList();
        if (ctrlNbrs.Count == 0) return [];
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var assignments = await uow.PositionAssignments.GetByStaffablePositionsAsync(ctrlNbrs!);
        return assignments.ToDictionary(a => a.StaffablePositionCtrlNbr);
    }

    // ── Eligibility ──────────────────────────────────────────────────────────

    public async Task<List<Employee>> GetEligibleEmployeesForRosterBoardAsync(
        ControlNumber craftCtrlNbr, ControlNumber clientCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var craftQualTypes = await uow.QualificationTypes.GetActiveByCraftCtrlNbrAsync(craftCtrlNbr);
        var craftQualTypeCtrlNbrs = craftQualTypes.Select(q => q.CtrlNbr).ToHashSet();
        if (craftQualTypeCtrlNbrs.Count == 0)
            return [];

        var employees = await uow.Employees.GetListByClientCtrlNbrAsync(clientCtrlNbr);
        if (employees.Count == 0)
            return [];

        var assignedCtrlNbrs = await uow.PositionAssignments.GetAssignedEmployeeCtrlNbrsAsync();
        var unassigned = assignedCtrlNbrs.Count == 0
            ? employees
            : [.. employees.Where(e => !assignedCtrlNbrs.Contains(e.CtrlNbr.Value))];

        var empQuals = await uow.EmployeeQualifications.GetActiveByEmployeeCtrlNbrsAsync(unassigned.Select(e => e.CtrlNbr));
        var qualifiedCtrlNbrs = empQuals
            .Where(eq => craftQualTypeCtrlNbrs.Contains(eq.QualificationTypeCtrlNbr))
            .Select(eq => eq.EmployeeCtrlNbr)
            .ToHashSet();

        return [.. unassigned.Where(e => qualifiedCtrlNbrs.Contains(e.CtrlNbr))];
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task ReconcileVacancyProjectionsForBoardOrderChangeAsync(
        ControlNumber boardCtrlNbr,
        CancellationToken ct)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr, ct);
        if (board?.RosterCtrlNbr is null)
            return;

        var roster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr, ct);
        if (roster?.WorkAreaGroupCtrlNbr is null)
            return;

        var incompleteShifts = await uow.ShiftInstances.GetIncompleteByWorkAreaAsync(roster.WorkAreaGroupCtrlNbr, ct);
        if (incompleteShifts.Count == 0)
            return;

        var boardOrderByPositionCtrlNbr = board.Positions
            .ToDictionary(position => position.CtrlNbr, position => position.PositionOrder);

        var boardOrderSyncChanged = false;

        foreach (var shift in incompleteShifts)
        {
            var shiftChanged = false;
            foreach (var boardSlot in shift.BoardSlots
                         .Where(slot => slot.RosterBoardCtrlNbr == board.CtrlNbr && slot.RosterBoardPositionCtrlNbr is not null))
            {
                if (!boardOrderByPositionCtrlNbr.TryGetValue(boardSlot.RosterBoardPositionCtrlNbr!, out var resolvedBoardOrder))
                    continue;

                if (boardSlot.BoardOrder == resolvedBoardOrder)
                    continue;

                boardSlot.SyncBoardOrder(resolvedBoardOrder);
                shiftChanged = true;
            }

            if (shiftChanged)
            {
                uow.ShiftInstances.Update(shift);
                boardOrderSyncChanged = true;
            }
        }

        var workInstanceStartsByCtrlNbr = new Dictionary<ControlNumber, DateTime>();
        foreach (var workInstanceCtrlNbr in incompleteShifts.Select(shift => shift.WorkInstanceCtrlNbr).Distinct())
        {
            var workInstance = await uow.WorkInstances.GetByCtrlNbrAsync(workInstanceCtrlNbr, ct);
            if (workInstance is not null)
            {
                workInstanceStartsByCtrlNbr[workInstanceCtrlNbr] = DateTime.SpecifyKind(workInstance.StartUtc, DateTimeKind.Utc);
            }
        }

        var anchorShift = incompleteShifts
            .Where(shift => workInstanceStartsByCtrlNbr.ContainsKey(shift.WorkInstanceCtrlNbr))
            .OrderBy(shift => workInstanceStartsByCtrlNbr[shift.WorkInstanceCtrlNbr])
            .ThenBy(shift => shift.CtrlNbr.Value)
            .FirstOrDefault();

        if (anchorShift is null)
        {
            if (boardOrderSyncChanged)
                await uow.CommitAsync(ct);
            return;
        }

        await vacancyProjectionSyncService.ReconcileFromShiftChangeAsync(uow, anchorShift, ct);
        await uow.CommitAsync(ct);
    }

    private static async Task<(RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName)>
        ResolveBoardDetailsAsync(IOrchestrationUnitOfWork uow, RosterBoard board, CancellationToken ct)
    {
        var craftName = string.Empty;
        var rosterName = string.Empty;
        long workAreaCtrlNbr = 0;
        var workAreaName = string.Empty;

        if (board.CraftCtrlNbr is not null)
        {
            var craft = await uow.Crafts.GetByCtrlNbrAsync(board.CraftCtrlNbr, ct);
            craftName = craft?.CraftName ?? string.Empty;
        }
        if (board.RosterCtrlNbr is not null)
        {
            var roster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr, ct);
            rosterName = roster?.RosterName ?? string.Empty;
            workAreaCtrlNbr = roster?.WorkAreaGroupCtrlNbr.Value ?? 0;
            if (roster?.WorkAreaGroupCtrlNbr is not null)
            {
                var group = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr, ct);
                workAreaName = group?.Name ?? string.Empty;
            }
        }
        return (board, craftName, rosterName, workAreaCtrlNbr, workAreaName);
    }

    private async Task<Dictionary<ControlNumber, List<string>>> ComputeRestrictionLabelsAsync(
        IOrchestrationUnitOfWork uow, ControlNumber? craftCtrlNbr,
        IEnumerable<ControlNumber> employeeCtrlNbrs, CancellationToken ct)
    {
        var empCtrlNbrs = employeeCtrlNbrs.Distinct().ToList();
        var result = new Dictionary<ControlNumber, List<string>>();
        if (empCtrlNbrs.Count == 0 || craftCtrlNbr is null) return result;

        var restrictingQualTypes = (await uow.QualificationTypes.GetActiveByCraftCtrlNbrAsync(craftCtrlNbr))
            .Where(qt => qt.RestrictionLabel is not null).ToList();
        if (restrictingQualTypes.Count == 0) return result;

        var empManualQuals = await uow.EmployeeQualifications.GetActiveByEmployeeCtrlNbrsAsync(empCtrlNbrs);
        var empActiveManualQualTypes = empManualQuals
            .GroupBy(eq => eq.EmployeeCtrlNbr)
            .ToDictionary(g => g.Key, g => g.Select(eq => eq.QualificationTypeCtrlNbr!).ToHashSet());

        var computedQualificationSatisfiedMap = new Dictionary<(long EmployeeCtrlNbr, long QualificationTypeCtrlNbr), bool>();

        foreach (var empCtrlNbr in empCtrlNbrs)
        {
            empActiveManualQualTypes.TryGetValue(empCtrlNbr, out var heldManualQuals);
            heldManualQuals ??= [];

            foreach (var qt in restrictingQualTypes)
            {
                var isHeld = string.Equals(qt.EvaluationStrategy, EvaluationStrategies.Manual, StringComparison.OrdinalIgnoreCase)
                    ? heldManualQuals.Contains(qt.CtrlNbr)
                    : await IsComputedQualificationSatisfiedAsync(empCtrlNbr, qt, uow, computedQualificationSatisfiedMap, ct);

                if (!isHeld)
                {
                    if (!result.TryGetValue(empCtrlNbr, out var labels))
                    {
                        labels = [];
                        result[empCtrlNbr] = labels;
                    }
                    labels.Add(qt.RestrictionLabel!);
                }
            }
        }
        return result;
    }

    public sealed record RemoveBoardPositionResult(
        ControlNumber BoardCtrlNbr,
        ControlNumber VacatedStaffablePositionCtrlNbr,
        ControlNumber? PreviousIncumbentCtrlNbr,
        bool IsExtraBoard);

    private async Task<Dictionary<ControlNumber, List<string>>> ComputeRestrictionLabelsAsync(
        IOrchestrationUnitOfWork uow, IEnumerable<ControlNumber> craftCtrlNbrs,
        IEnumerable<ControlNumber> employeeCtrlNbrs, CancellationToken ct)
    {
        var empCtrlNbrs = employeeCtrlNbrs.Distinct().ToList();
        var result = new Dictionary<ControlNumber, List<string>>();
        if (empCtrlNbrs.Count == 0) return result;

        var allRestrictingQualTypes = (await Task.WhenAll(
            craftCtrlNbrs.Select(c => uow.QualificationTypes.GetActiveByCraftCtrlNbrAsync(c))))
            .SelectMany(list => list)
            .Where(qt => qt.RestrictionLabel is not null)
            .ToList();
        if (allRestrictingQualTypes.Count == 0) return result;

        var empManualQuals = await uow.EmployeeQualifications.GetActiveByEmployeeCtrlNbrsAsync(empCtrlNbrs);
        var empActiveManualQualTypes = empManualQuals
            .GroupBy(eq => eq.EmployeeCtrlNbr)
            .ToDictionary(g => g.Key, g => g.Select(eq => eq.QualificationTypeCtrlNbr!).ToHashSet());

        var computedQualificationSatisfiedMap = new Dictionary<(long EmployeeCtrlNbr, long QualificationTypeCtrlNbr), bool>();

        foreach (var empCtrlNbr in empCtrlNbrs)
        {
            empActiveManualQualTypes.TryGetValue(empCtrlNbr, out var heldManualQuals);
            heldManualQuals ??= [];

            foreach (var qt in allRestrictingQualTypes)
            {
                var isHeld = string.Equals(qt.EvaluationStrategy, EvaluationStrategies.Manual, StringComparison.OrdinalIgnoreCase)
                    ? heldManualQuals.Contains(qt.CtrlNbr)
                    : await IsComputedQualificationSatisfiedAsync(empCtrlNbr, qt, uow, computedQualificationSatisfiedMap, ct);

                if (!isHeld)
                {
                    if (!result.TryGetValue(empCtrlNbr, out var labels))
                    {
                        labels = [];
                        result[empCtrlNbr] = labels;
                    }
                    labels.Add(qt.RestrictionLabel!);
                }
            }
        }
        return result;
    }

    private async Task<bool> IsComputedQualificationSatisfiedAsync(
        ControlNumber employeeCtrlNbr,
        QualificationType qualificationType,
        IOrchestrationUnitOfWork uow,
        Dictionary<(long EmployeeCtrlNbr, long QualificationTypeCtrlNbr), bool> computedQualificationSatisfiedMap,
        CancellationToken ct)
    {
        var key = (employeeCtrlNbr.Value, qualificationType.CtrlNbr.Value);
        if (computedQualificationSatisfiedMap.TryGetValue(key, out var cached))
            return cached;

        var evaluation = await requirementEvaluationService.EvaluateAsync(employeeCtrlNbr, qualificationType, uow, ct);
        var isSatisfied = evaluation.AllSatisfied && !evaluation.IsSuspended;
        computedQualificationSatisfiedMap[key] = isSatisfied;
        return isSatisfied;
    }

    /// <summary>
    /// Resolves the craft's required-positions strategy (or the board-level override),
    /// calculates the current threshold, and persists it back to the board if it changed.
    /// Only called for ExtraBoard boards.
    /// </summary>
    private async Task RecalculateRequiredPositionsAsync(
        IOrchestrationUnitOfWork uow,
        RosterBoard board,
        ControlNumber workAreaCtrlNbr,
        CancellationToken ct)
    {
        // Prefer a board-level strategy override, then fall back to the craft assignment.
        RequiredPositionsStrategy? strategy = null;

        if (board.RequiredPositionsStrategyCtrlNbr is not null)
        {
            strategy = await uow.RequiredPositionsStrategies.GetByCtrlNbrAsync(board.RequiredPositionsStrategyCtrlNbr, ct);
        }

        if (strategy is null && board.CraftCtrlNbr is not null)
        {
            var craftAssignment = await uow.CraftRequiredPositionsStrategies.GetByCraftAsync(board.CraftCtrlNbr, ct);
            if (craftAssignment is not null)
                strategy = await uow.RequiredPositionsStrategies.GetByCtrlNbrAsync(craftAssignment.StrategyCtrlNbr, ct);
        }

        if (strategy is null)
            return;

        // The Static formula is a manual, no-op strategy: its RequiredPositions value is set
        // explicitly by an admin and must never be recalculated (Calculate always returns 0,
        // which would wipe out the manual value and disable auto-bulletining). Skip it entirely.
        if (string.Equals(strategy.FormulaType, FormulaTypes.Static, StringComparison.OrdinalIgnoreCase))
            return;

        var formula = formulaRegistry.GetFormula(strategy.FormulaType);
        if (formula is null)
            return;

        var avgVacancies = await uow.PositionVacancies.GetAverageDailyBoardVacanciesAsync(workAreaCtrlNbr, board.CraftCtrlNbr!, ct);
        var parameters = strategy.GetParameters();
        var newRequired = formula.Calculate(avgVacancies, parameters);

        if (board.RequiredPositions != newRequired)
        {
            board.UpdateRequiredPositions(newRequired);
            uow.RosterBoards.Update(board);
        }
    }
}
