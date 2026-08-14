using CrewService.Application.Policies;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Workflows;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Workflows.Effects;

public sealed class CreateSeniorityMoveWorkflowDatabaseEffect(
    ILogger<CreateSeniorityMoveWorkflowDatabaseEffect> logger) : IDatabaseWorkflowEffect
{
    public string EffectTypeCode => WorkflowEffectTypeCodes.CreateSeniorityMove;

    public async Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteAsync(WorkflowEffectExecutionContext context)
    {
        var runtime = context.RuntimeContext;
        if (runtime.EmployeeCtrlNbr is null)
            return [];

        var targetBoardType = ResolveBoardType(context.Effect);
        var autoMoveDelayHours = ResolveAutoMoveDelayHours(context.Effect);

        var employeeCtrlNbr = runtime.EmployeeCtrlNbr;
        var assignments = await context.Uow.PositionAssignments.GetByEmployeeAsync(employeeCtrlNbr);
        var currentAssignment = assignments
            .OrderByDescending(a => a.AssignedDateUtc)
            .FirstOrDefault();
        if (currentAssignment is null)
            return [];

        var currentBoard = currentAssignment.AssignmentSourceCtrlNbr is not null
            ? await context.Uow.RosterBoards.GetByPositionCtrlNbrAsync(currentAssignment.AssignmentSourceCtrlNbr, context.CancellationToken)
            : null;

        currentBoard ??= await context.Uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(currentAssignment.StaffablePositionCtrlNbr, context.CancellationToken);
        if (currentBoard is null)
            return [];

        var craftBoards = await context.Uow.RosterBoards.GetByCraftCtrlNbrAsync(currentBoard.CraftCtrlNbr, context.CancellationToken);
        var targetBoard = craftBoards
            .FirstOrDefault(board => board.IsActive && board.BoardType == targetBoardType);

        if (targetBoard is null)
        {
            logger.LogInformation(
                "WorkflowRuntimeService: Skipping Create Seniority Move effect because board type {BoardType} is not active for craft {CraftCtrlNbr}.",
                targetBoardType,
                currentBoard.CraftCtrlNbr.Value);
            return [];
        }

        if (currentBoard.CtrlNbr == targetBoard.CtrlNbr)
            return [];

        var existingMoves = await context.Uow.SeniorityMoves.GetByEmployeeAsync(employeeCtrlNbr, context.CancellationToken);
        var activeHangoutMoves = existingMoves
            .Where(move =>
                move.MoveType == SeniorityMoveType.Hangout
                && (move.Status == SeniorityMoveStatus.Pending || move.Status == SeniorityMoveStatus.Approved))
            .ToList();

        foreach (var activeMove in activeHangoutMoves)
        {
            activeMove.Cancel("Superseded by a newer workflow-created seniority move.");
            await context.Uow.SeniorityMoves.UpdateAsync(activeMove, context.CancellationToken);
        }

        var daysOnCurrentPosition = (int)(DateTime.UtcNow - currentAssignment.AssignedDateUtc).TotalDays;
        var effectiveUtc = DateTime.UtcNow.AddHours(autoMoveDelayHours);

        await PoliciesService.StageSeniorityMoveAsync(
            context.Uow,
            runtime.TriggerRailroadCtrlNbr,
            employeeCtrlNbr,
            currentBoard.CraftCtrlNbr,
            targetBoard.CtrlNbr,
            displacedEmployeeCtrlNbr: null,
            daysOnCurrentPosition,
            moveType: SeniorityMoveType.Hangout,
            effectiveUtc,
            willWork: null,
            autoApprove: true,
            context.CancellationToken);

        return [];
    }

    private static int ResolveAutoMoveDelayHours(WorkflowEffectDefinition effect)
    {
        var value = TryGetOption(effect, WorkflowOptionKeys.AutoMoveDelayHours);
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        if (!int.TryParse(value, out var delayHours) || delayHours < 0)
            throw new InvalidOperationException("Create Seniority Move effect requires autoMoveDelayHours greater than or equal to 0.");

        return delayHours;
    }

    private static BoardType ResolveBoardType(WorkflowEffectDefinition effect)
    {
        var boardOption = TryGetOption(effect, WorkflowOptionKeys.BoardType);
        if (string.IsNullOrWhiteSpace(boardOption))
            throw new InvalidOperationException("Create Seniority Move effect requires a board type.");

        return boardOption.Trim() switch
        {
            "Extra Board" => BoardType.ExtraBoard,
            "Hangout" => BoardType.Hangout,
            "Extended Absence" => BoardType.ExtendedAbsence,
            "Training" => BoardType.Training,
            "New Hire" => BoardType.NewHire,
            "New Hires" => BoardType.NewHire,
            _ => throw new InvalidOperationException($"Unsupported board type '{boardOption}'.")
        };
    }

    private static string? TryGetOption(WorkflowEffectDefinition effect, string key)
    {
        if (effect.Options.TryGetValue(key, out var directValue))
            return directValue;

        if (string.Equals(key, WorkflowOptionKeys.BoardType, StringComparison.OrdinalIgnoreCase)
            && effect.Options.TryGetValue(WorkflowOptionKeys.EffectOption, out var effectOptionValue))
        {
            return effectOptionValue;
        }

        return null;
    }
}
