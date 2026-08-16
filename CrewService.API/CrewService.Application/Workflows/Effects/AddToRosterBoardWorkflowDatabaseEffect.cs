using CrewService.Application.RosterBoardOps;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Workflows.Effects;

public sealed class AddToRosterBoardWorkflowDatabaseEffect(
    SeniorityWorkflowAssignmentPath assignmentPath,
    RosterBoardAppService rosterBoardAppService,
    ILogger<AddToRosterBoardWorkflowDatabaseEffect> logger) : IDatabaseWorkflowEffect
{
    public string EffectTypeCode => WorkflowEffectTypeCodes.AddToRosterBoard;

    public async Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteAsync(WorkflowEffectExecutionContext context)
    {
        var runtime = context.RuntimeContext;
        if (runtime.EmployeeCtrlNbr is null)
            return [];

        var boardType = ResolveBoardType(context.Effect);
        var employeeCtrlNbr = runtime.EmployeeCtrlNbr;
        var rosterCtrlNbr = runtime.RosterCtrlNbr;
        if (rosterCtrlNbr is null)
        {
            logger.LogInformation(
                "WorkflowRuntimeService: Skipping Add to Roster Board effect because roster context is missing for employee {EmployeeCtrlNbr}.",
                employeeCtrlNbr.Value);
            return [];
        }

        var roster = await context.Uow.Rosters.GetByCtrlNbrAsync(rosterCtrlNbr, context.CancellationToken);
        if (roster?.CraftCtrlNbr is null)
            return [];

        var boards = await context.Uow.RosterBoards.GetByCraftCtrlNbrAsync(roster.CraftCtrlNbr, context.CancellationToken);
        var board = boards
            .Where(b => b.RosterCtrlNbr == rosterCtrlNbr && b.IsActive && b.BoardType == boardType)
            .OrderBy(b => b.CtrlNbr.Value)
            .FirstOrDefault();

        if (board is null)
        {
            logger.LogInformation(
                "WorkflowRuntimeService: Skipping Add to Roster Board effect because board type {BoardType} is not active on roster {RosterCtrlNbr}.",
                boardType,
                rosterCtrlNbr.Value);
            return [];
        }

        if (await IsEmployeeAlreadyOnBoardTypeAsync(context, employeeCtrlNbr, rosterCtrlNbr, boardType))
            return [];

        var vacateResults = await assignmentPath.VacateEmployeeAssignmentsAsync(
            context.Uow,
            employeeCtrlNbr,
            context.CancellationToken);

        var nextOrder = board.Positions.Count > 0 ? board.Positions.Max(p => p.PositionOrder) + 1 : 1;
        await rosterBoardAppService.AddRosterBoardPositionInOrchestrationAsync(
            context.Uow,
            board.CtrlNbr,
            employeeCtrlNbr,
            nextOrder,
            assignedDateUtc: null,
            context.CancellationToken);

        return SeniorityWorkflowPostCommitWorkBuilder.BuildVacancyRepostWorkItems(vacateResults);
    }

    private static BoardType ResolveBoardType(WorkflowEffectDefinition effect)
    {
        if (!effect.Options.TryGetValue(WorkflowOptionKeys.EffectOption, out var boardOption)
            || string.IsNullOrWhiteSpace(boardOption))
        {
            throw new InvalidOperationException("Add to Roster Board effect requires an effectOption specifying the board type.");
        }

        return boardOption.Trim() switch
        {
            "Extra Board" => BoardType.ExtraBoard,
            "Hangout" => BoardType.Hangout,
            "Extended Absence" => BoardType.ExtendedAbsence,
            "Training" => BoardType.Training,
            "New Hire" => BoardType.NewHire,
            "New Hires" => BoardType.NewHire,
            _ => throw new InvalidOperationException($"Unsupported roster board option '{boardOption}'.")
        };
    }

    private static async Task<bool> IsEmployeeAlreadyOnBoardTypeAsync(
        WorkflowEffectExecutionContext context,
        ControlNumber employeeCtrlNbr,
        ControlNumber rosterCtrlNbr,
        BoardType boardType)
    {
        var assignments = await context.Uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        foreach (var assignment in assignments)
        {
            var staffablePosition = await context.Uow.StaffablePositions.GetByCtrlNbrAsync(
                assignment.StaffablePositionCtrlNbr,
                context.CancellationToken);
            if (staffablePosition?.PositionType != StaffablePositionType.Board)
                continue;

            RosterBoard? board = null;
            if (assignment.AssignmentSourceCtrlNbr is not null)
            {
                board = await context.Uow.RosterBoards.GetByPositionCtrlNbrAsync(
                    assignment.AssignmentSourceCtrlNbr,
                    context.CancellationToken);
            }

            if (assignment.AssignmentSourceCtrlNbr is null)
            {
                throw new InvalidOperationException(
                    $"Board assignment source is missing for employee {employeeCtrlNbr.Value} on staffable position {assignment.StaffablePositionCtrlNbr.Value}.");
            }

            if (board is null)
            {
                throw new InvalidOperationException(
                    $"Board position {assignment.AssignmentSourceCtrlNbr.Value} was not found for employee {employeeCtrlNbr.Value} assignment.");
            }

            var hasMatchingPosition = board.Positions.Any(p => p.StaffablePositionCtrlNbr == assignment.StaffablePositionCtrlNbr);
            if (!hasMatchingPosition)
                throw new InvalidOperationException(
                    $"Board position {assignment.AssignmentSourceCtrlNbr.Value} does not match staffable position {assignment.StaffablePositionCtrlNbr.Value} for employee {employeeCtrlNbr.Value}.");

            if (board?.RosterCtrlNbr == rosterCtrlNbr && board.BoardType == boardType)
                return true;
        }

        return false;
    }
}