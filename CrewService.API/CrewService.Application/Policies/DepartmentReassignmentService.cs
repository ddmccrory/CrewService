using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.ValueObjects;
using CrewService.Application.Notifications;

namespace CrewService.Application.Policies;

public sealed class DepartmentReassignmentService(
    EmployeeNotificationService? notifications = null)
{
    public async Task ReassignEmployeeAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        ControlNumber departmentCtrlNbr,
        CancellationToken ct = default)
    {
        var rule = await uow.DepartmentReassignmentRules.GetByDepartmentAsync(departmentCtrlNbr);
        if (rule is null)
            throw new InvalidOperationException($"Department reassignment rule is not configured for department {departmentCtrlNbr.Value}.");

        var crafts = await uow.Crafts.GetAllAsync(ct);
        var departmentCraftCtrlNbrs = crafts
            .Where(c => c.DepartmentCtrlNbr == departmentCtrlNbr)
            .Select(c => c.CtrlNbr)
            .Distinct()
            .ToList();

        var employeeCraftCtrlNbr = await ResolveEmployeeCraftCtrlNbrAsync(uow, employeeCtrlNbr, ct);
        var craftCtrlNbrs = employeeCraftCtrlNbr is not null && departmentCraftCtrlNbrs.Contains(employeeCraftCtrlNbr)
            ? [employeeCraftCtrlNbr]
            : departmentCraftCtrlNbrs;

        var boards = new List<RosterBoard>();
        foreach (var craftCtrlNbr in craftCtrlNbrs)
        {
            var byCraft = await uow.RosterBoards.GetByCraftCtrlNbrAsync(craftCtrlNbr, ct);
            boards.AddRange(byCraft);
        }

        var targetBoard = boards
            .FirstOrDefault(b => b.IsActive && b.BoardType == rule.TargetBoardType);

        if (targetBoard is null)
        {
            if (rule.IsRequired)
                throw new InvalidOperationException(
                    $"No active {rule.TargetBoardType} board found for department {departmentCtrlNbr.Value}. Reassignment is required.");

            return;
        }

        if (targetBoard.Positions.Any(p => p.EmployeeCtrlNbr == employeeCtrlNbr))
            return;

        var staffablePosition = StaffablePosition.Create(StaffablePositionType.Board);
        uow.StaffablePositions.Add(staffablePosition);

        var nextOrder = targetBoard.Positions.Count > 0
            ? targetBoard.Positions.Max(p => p.PositionOrder) + 1
            : 1;

        var boardPosition = targetBoard.AddPosition(employeeCtrlNbr, nextOrder, staffablePosition.CtrlNbr);
        uow.RosterBoards.Update(targetBoard);

        var assignment = PositionAssignment.Create(
            staffablePosition.CtrlNbr,
            employeeCtrlNbr,
            PositionAssignmentType.Board,
            assignmentSourceCtrlNbr: boardPosition.CtrlNbr);
        uow.PositionAssignments.Add(assignment);

        if (notifications is not null)
        {
            await notifications.NotifyBoardPlacementAsync(
                uow,
                targetBoard,
                employeeCtrlNbr,
                subject: null,
                ct);
        }
    }

    private static async Task<ControlNumber?> ResolveEmployeeCraftCtrlNbrAsync(
        IOrchestrationUnitOfWork uow,
        ControlNumber employeeCtrlNbr,
        CancellationToken ct)
    {
        var assignments = await uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        var currentAssignment = assignments
            .OrderByDescending(a => a.AssignedDateUtc)
            .FirstOrDefault();

        if (currentAssignment is null)
            return null;

        var staffablePosition = await uow.StaffablePositions.GetByCtrlNbrAsync(currentAssignment.StaffablePositionCtrlNbr, ct);
        if (staffablePosition is null)
            return null;

        if (staffablePosition.PositionType == StaffablePositionType.Board)
        {
            var board = currentAssignment.AssignmentSourceCtrlNbr is not null
                ? await uow.RosterBoards.GetByPositionCtrlNbrAsync(currentAssignment.AssignmentSourceCtrlNbr, ct)
                : null;

            board ??= await uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(currentAssignment.StaffablePositionCtrlNbr, ct);
            return board?.CraftCtrlNbr;
        }

        if (staffablePosition.PositionType == StaffablePositionType.Crew)
        {
            var crewPosition = currentAssignment.AssignmentSourceCtrlNbr is not null
                ? await uow.CrewPositions.GetByCtrlNbrAsync(currentAssignment.AssignmentSourceCtrlNbr, ct)
                : await uow.CrewPositions.GetByStaffablePositionAsync(currentAssignment.StaffablePositionCtrlNbr);

            if (crewPosition is null)
                return null;

            var craftRole = await uow.CraftRoles.GetByCtrlNbrAsync(crewPosition.CraftRoleCtrlNbr, ct);
            return craftRole?.CraftCtrlNbr;
        }

        return null;
    }
}