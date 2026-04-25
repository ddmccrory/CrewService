using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.RosterBoardOps;

public sealed class RosterBoardAppService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    // ── Single Board ─────────────────────────────────────────────────────────

    public async Task<(RosterBoard? Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName, Dictionary<ControlNumber, List<string>> RestrictionLabels)>
        GetRosterBoardDetailAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(ctrlNbr);
        if (board is null)
            return (null, string.Empty, string.Empty, 0, string.Empty, []);

        var craftName = string.Empty;
        var rosterName = string.Empty;
        long workAreaCtrlNbr = 0;
        var workAreaName = string.Empty;

        if (board.CraftCtrlNbr is not null)
        {
            var craft = await uow.Crafts.GetByCtrlNbrAsync(board.CraftCtrlNbr);
            craftName = craft?.CraftName ?? string.Empty;
        }

        if (board.RosterCtrlNbr is not null)
        {
            var roster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr);
            rosterName = roster?.RosterName ?? string.Empty;
            workAreaCtrlNbr = roster?.WorkAreaGroupCtrlNbr.Value ?? 0;
            if (roster?.WorkAreaGroupCtrlNbr is not null)
            {
                var group = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr);
                workAreaName = group?.Name ?? string.Empty;
            }
        }

        var positionEmps = board.Positions.Select(p => p.EmployeeCtrlNbr).Where(e => e is not null).Distinct().ToList();
        var restrictionLabels = await ComputeRestrictionLabelsAsync(uow, board.CraftCtrlNbr, positionEmps!, ct);
        return (board, craftName, rosterName, workAreaCtrlNbr, workAreaName, restrictionLabels);
    }

    // ── Board List ───────────────────────────────────────────────────────────

    public sealed record BoardListResult(
        IReadOnlyList<RosterBoard> Boards,
        Dictionary<ControlNumber, string> CraftNames,
        Dictionary<ControlNumber, Roster> RosterMap,
        Dictionary<ControlNumber, string> GroupNames,
        Dictionary<ControlNumber, Employee> EmployeeMap,
        Dictionary<ControlNumber, List<string>> RestrictionLabels);

    public async Task<BoardListResult> GetAllRosterBoardsAsync(
        long craftCtrlNbr, long parentCtrlNbr, long dynamicGroupCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        IReadOnlyList<RosterBoard> boards;
        if (craftCtrlNbr > 0)
        {
            boards = await uow.RosterBoards.GetByCraftCtrlNbrAsync(ControlNumber.Create(craftCtrlNbr));
        }
        else if (parentCtrlNbr > 0)
        {
            var crafts = await uow.Crafts.GetByParentAndRailroadAsync(
                ControlNumber.Create(parentCtrlNbr),
                dynamicGroupCtrlNbr > 0 ? ControlNumber.Create(dynamicGroupCtrlNbr) : null);
            var craftCtrlNbrs = crafts.Select(c => c.CtrlNbr).ToList();
            boards = craftCtrlNbrs.Count > 0 ? await uow.RosterBoards.GetByCraftCtrlNbrsAsync(craftCtrlNbrs) : [];
        }
        else
        {
            boards = await uow.RosterBoards.GetAllAsync();
        }

        if (boards.Count == 0)
            return new BoardListResult(boards, [], [], [], [], []);

        var distinctCraftCtrlNbrs = boards.Select(b => b.CraftCtrlNbr).Where(c => c is not null).Distinct().ToList();
        var allPositionEmployeeCtrlNbrs = boards.SelectMany(b => b.Positions)
            .Select(p => p.EmployeeCtrlNbr).Where(e => e is not null).Distinct().ToList();

        var craftTasks = await Task.WhenAll(distinctCraftCtrlNbrs.Select(c => uow.Crafts.GetByCtrlNbrAsync(c!)));
        var rosters = distinctCraftCtrlNbrs.Count > 0
            ? await uow.Rosters.GetByCraftCtrlNbrsAsync(distinctCraftCtrlNbrs!)
            : new List<Roster>();
        var employees = allPositionEmployeeCtrlNbrs.Count > 0
            ? await uow.Employees.GetByCtrlNbrsAsync(allPositionEmployeeCtrlNbrs!)
            : new List<Employee>();

        var craftMap = craftTasks.Where(c => c is not null).ToDictionary(c => c!.CtrlNbr, c => c!.CraftName);
        var rosterMap = rosters.ToDictionary(r => r.CtrlNbr, r => r);
        var employeeMap = employees.ToDictionary(e => e.CtrlNbr, e => e);

        var distinctWorkAreaCtrlNbrs = rosterMap.Values.Select(r => r.WorkAreaGroupCtrlNbr).Distinct().ToList();
        var groups = distinctWorkAreaCtrlNbrs.Count > 0
            ? await uow.DynamicGroups.GetByCtrlNbrsAsync(distinctWorkAreaCtrlNbrs)
            : new List<DynamicGroup>();
        var groupMap = groups.ToDictionary(g => g.CtrlNbr, g => g.Name);

        var restrictionLabels = await ComputeRestrictionLabelsAsync(
            uow, distinctCraftCtrlNbrs!, allPositionEmployeeCtrlNbrs!, ct);

        return new BoardListResult(boards, craftMap, rosterMap, groupMap, employeeMap, restrictionLabels);
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<(RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName)>
        CreateRosterBoardAsync(long craftCtrlNbr, long rosterCtrlNbr, string name,
            BoardType boardType, RotationType rotationType, bool isActive, CancellationToken ct = default)
    {
        var board = RosterBoard.Create(
            ControlNumber.Create(craftCtrlNbr), ControlNumber.Create(rosterCtrlNbr),
            name, boardType, rotationType, isActive);

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.RosterBoards.Add(board);
        var createResult = await ResolveBoardDetailsAsync(uow, board, ct);
        await uow.CommitAsync(ct);

        return createResult;
    }

    public async Task<(RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName)>
        UpdateRosterBoardAsync(ControlNumber ctrlNbr, string name,
            BoardType boardType, RotationType rotationType, bool isActive, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Roster board {ctrlNbr.Value} not found.");
        board.Update(name, boardType, rotationType, isActive);
        uow.RosterBoards.Update(board);
        var updateResult = await ResolveBoardDetailsAsync(uow, board, ct);
        await uow.CommitAsync(ct);
        return updateResult;
    }

    public async Task<ControlNumber?> DeleteRosterBoardAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Roster board {ctrlNbr.Value} not found.");
        uow.RosterBoards.Remove(board);
        await uow.CommitAsync(ct);
        return board.CtrlNbr;
    }

    // ── Positions ────────────────────────────────────────────────────────────

    public async Task<(RosterBoardPosition Position, Dictionary<ControlNumber, List<string>> RestrictionLabels)>
        AddRosterBoardPositionAsync(ControlNumber boardCtrlNbr, ControlNumber employeeCtrlNbr,
            int positionOrder, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr)
            ?? throw new KeyNotFoundException($"Roster board {boardCtrlNbr.Value} not found.");

        var existingAssignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        if (existingAssignments.Count > 0)
            throw new InvalidOperationException(
                "This employee is already assigned to a staffable position. Unassign them first.");

        var staffablePosition = StaffablePosition.Create("Board");
        var position = board.AddPosition(employeeCtrlNbr, positionOrder, staffablePosition.CtrlNbr);
        var positionAssignment = PositionAssignment.Create(
            staffablePosition.CtrlNbr, employeeCtrlNbr, "Board", position.CtrlNbr);

        uow.StaffablePositions.Add(staffablePosition);
        uow.PositionAssignments.Add(positionAssignment);
        uow.RosterBoards.Update(board);
        var labels = await ComputeRestrictionLabelsAsync(uow, board.CraftCtrlNbr, [employeeCtrlNbr], ct);
        await uow.CommitAsync(ct);
        return (position, labels);
    }

    public async Task<ControlNumber> RemoveRosterBoardPositionAsync(ControlNumber positionCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var boards = await uow.RosterBoards.GetAllAsync();
        var board = boards.FirstOrDefault(b => b.Positions.Any(p => p.CtrlNbr == positionCtrlNbr))
            ?? throw new KeyNotFoundException($"Position {positionCtrlNbr.Value} not found on any board.");

        var position = board.Positions.First(p => p.CtrlNbr == positionCtrlNbr);
        var positionAssignment = await uow.PositionAssignments.GetByStaffablePositionAsync(position.StaffablePositionCtrlNbr);

        board.RemovePosition(position);
        uow.RosterBoards.Update(board);
        if (positionAssignment is not null)
            uow.PositionAssignments.Remove(positionAssignment);
        await uow.CommitAsync(ct);
        return positionCtrlNbr;
    }

    public async Task<(RosterBoardPosition Position, Dictionary<ControlNumber, List<string>> RestrictionLabels)>
        HangoutPositionAsync(ControlNumber positionCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var boards = await uow.RosterBoards.GetAllAsync();
        var board = boards.FirstOrDefault(b => b.Positions.Any(p => p.CtrlNbr == positionCtrlNbr))
            ?? throw new KeyNotFoundException($"Position {positionCtrlNbr.Value} not found.");
        var position = board.Positions.First(p => p.CtrlNbr == positionCtrlNbr);
        var labels = await ComputeRestrictionLabelsAsync(uow, board.CraftCtrlNbr, [position.EmployeeCtrlNbr], ct);
        return (position, labels);
    }

    public async Task<(RosterBoardPosition Position, Dictionary<ControlNumber, List<string>> RestrictionLabels)>
        RestorePositionAsync(ControlNumber positionCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var boards = await uow.RosterBoards.GetAllAsync();
        var board = boards.FirstOrDefault(b => b.Positions.Any(p => p.CtrlNbr == positionCtrlNbr))
            ?? throw new KeyNotFoundException($"Position {positionCtrlNbr.Value} not found.");
        var position = board.Positions.First(p => p.CtrlNbr == positionCtrlNbr);
        position.RestoreFromHangout();
        uow.RosterBoards.Update(board);
        var labels = await ComputeRestrictionLabelsAsync(uow, board.CraftCtrlNbr, [position.EmployeeCtrlNbr], ct);
        await uow.CommitAsync(ct);
        return (position, labels);
    }

    public async Task<(RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName)>
        ReorderRosterBoardPositionsAsync(ControlNumber boardCtrlNbr,
            List<(ControlNumber PositionCtrlNbr, int PositionOrder)> ordering, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var board = await uow.RosterBoards.GetByCtrlNbrAsync(boardCtrlNbr)
            ?? throw new KeyNotFoundException($"Roster board {boardCtrlNbr.Value} not found.");
        board.ReorderPositions(ordering);
        uow.RosterBoards.Update(board);
        var reorderResult = await ResolveBoardDetailsAsync(uow, board, ct);
        await uow.CommitAsync(ct);
        return reorderResult;
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
            : employees.Where(e => !assignedCtrlNbrs.Contains(e.CtrlNbr.Value)).ToList();

        var empQuals = await uow.EmployeeQualifications.GetActiveByEmployeeCtrlNbrsAsync(unassigned.Select(e => e.CtrlNbr));
        var qualifiedCtrlNbrs = empQuals
            .Where(eq => craftQualTypeCtrlNbrs.Contains(eq.QualificationTypeCtrlNbr))
            .Select(eq => eq.EmployeeCtrlNbr)
            .ToHashSet();

        return unassigned.Where(e => qualifiedCtrlNbrs.Contains(e.CtrlNbr)).ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<(RosterBoard Board, string CraftName, string RosterName, long WorkAreaCtrlNbr, string WorkAreaName)>
        ResolveBoardDetailsAsync(IOrchestrationUnitOfWork uow, RosterBoard board, CancellationToken ct)
    {
        var craftName = string.Empty;
        var rosterName = string.Empty;
        long workAreaCtrlNbr = 0;
        var workAreaName = string.Empty;

        if (board.CraftCtrlNbr is not null)
        {
            var craft = await uow.Crafts.GetByCtrlNbrAsync(board.CraftCtrlNbr);
            craftName = craft?.CraftName ?? string.Empty;
        }
        if (board.RosterCtrlNbr is not null)
        {
            var roster = await uow.Rosters.GetByCtrlNbrAsync(board.RosterCtrlNbr);
            rosterName = roster?.RosterName ?? string.Empty;
            workAreaCtrlNbr = roster?.WorkAreaGroupCtrlNbr.Value ?? 0;
            if (roster?.WorkAreaGroupCtrlNbr is not null)
            {
                var group = await uow.DynamicGroups.GetByCtrlNbrAsync(roster.WorkAreaGroupCtrlNbr);
                workAreaName = group?.Name ?? string.Empty;
            }
        }
        return (board, craftName, rosterName, workAreaCtrlNbr, workAreaName);
    }

    private static async Task<Dictionary<ControlNumber, List<string>>> ComputeRestrictionLabelsAsync(
        IOrchestrationUnitOfWork uow, ControlNumber? craftCtrlNbr,
        IEnumerable<ControlNumber> employeeCtrlNbrs, CancellationToken ct)
    {
        var empCtrlNbrs = employeeCtrlNbrs.Distinct().ToList();
        var result = new Dictionary<ControlNumber, List<string>>();
        if (empCtrlNbrs.Count == 0 || craftCtrlNbr is null) return result;

        var restrictingQualTypes = (await uow.QualificationTypes.GetActiveByCraftCtrlNbrAsync(craftCtrlNbr))
            .Where(qt => qt.RestrictionLabel is not null).ToList();
        if (restrictingQualTypes.Count == 0) return result;

        var empQuals = await uow.EmployeeQualifications.GetActiveByEmployeeCtrlNbrsAsync(empCtrlNbrs);
        var empActiveQualTypes = empQuals
            .GroupBy(eq => eq.EmployeeCtrlNbr)
            .ToDictionary(g => g.Key, g => g.Select(eq => eq.QualificationTypeCtrlNbr!).ToHashSet());

        foreach (var empCtrlNbr in empCtrlNbrs)
        {
            empActiveQualTypes.TryGetValue(empCtrlNbr, out var heldQuals);
            heldQuals ??= [];
            foreach (var qt in restrictingQualTypes)
            {
                if (!heldQuals.Contains(qt.CtrlNbr))
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

    private static async Task<Dictionary<ControlNumber, List<string>>> ComputeRestrictionLabelsAsync(
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

        var empQuals = await uow.EmployeeQualifications.GetActiveByEmployeeCtrlNbrsAsync(empCtrlNbrs);
        var empActiveQualTypes = empQuals
            .GroupBy(eq => eq.EmployeeCtrlNbr)
            .ToDictionary(g => g.Key, g => g.Select(eq => eq.QualificationTypeCtrlNbr!).ToHashSet());

        foreach (var empCtrlNbr in empCtrlNbrs)
        {
            empActiveQualTypes.TryGetValue(empCtrlNbr, out var heldQuals);
            heldQuals ??= [];
            foreach (var qt in allRestrictingQualTypes)
            {
                if (!heldQuals.Contains(qt.CtrlNbr))
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
}
