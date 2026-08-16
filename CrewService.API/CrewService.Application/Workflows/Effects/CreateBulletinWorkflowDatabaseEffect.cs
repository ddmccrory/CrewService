using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.Workflows;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.Workflows.Effects;

public sealed class CreateBulletinWorkflowDatabaseEffect(
    ILogger<CreateBulletinWorkflowDatabaseEffect> logger) : IDatabaseWorkflowEffect
{
    public string EffectTypeCode => WorkflowEffectTypeCodes.CreateBulletin;

    public async Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteAsync(WorkflowEffectExecutionContext context)
    {
        var payload = context.RuntimeContext.PositionVacatedPayload;
        if (payload is null)
            return [];

        if (payload.PositionTypeCode == StaffablePositionType.Crew)
        {
            return
            [
                new WorkflowEffectPostCommitWorkItem(
                    WorkflowPostCommitWorkTypes.RepostVacatedPosition,
                    new RepostVacatedPositionPostCommitPayload(
                        payload.StaffablePositionCtrlNbr,
                        payload.PreviousIncumbentCtrlNbr))
            ];
        }

        if (payload.PositionTypeCode == StaffablePositionType.Board)
        {
            var board = payload.BoardCtrlNbr is not null
                ? await context.Uow.RosterBoards.GetByCtrlNbrAsync(payload.BoardCtrlNbr, context.CancellationToken)
                : await context.Uow.RosterBoards.GetByStaffablePositionCtrlNbrAsync(
                    payload.StaffablePositionCtrlNbr,
                    context.CancellationToken);
            if (board is null)
            {
                logger.LogInformation(
                    "WorkflowRuntimeService: Skipping Create Bulletin effect for board position {StaffablePositionCtrlNbr} because no board mapping was found.",
                    payload.StaffablePositionCtrlNbr.Value);
                return [];
            }

            return
            [
                new WorkflowEffectPostCommitWorkItem(
                    WorkflowPostCommitWorkTypes.RepostBoardPositionIfUnderstaffed,
                    new RepostBoardPositionIfUnderstaffedPostCommitPayload(
                        board.CtrlNbr,
                        payload.StaffablePositionCtrlNbr,
                        payload.PreviousIncumbentCtrlNbr,
                        EnforceUnderstaffedPolicy: true))
            ];
        }

        logger.LogInformation(
            "WorkflowRuntimeService: Skipping Create Bulletin effect for position {StaffablePositionCtrlNbr} due to unsupported position type '{PositionTypeCode}'.",
            payload.StaffablePositionCtrlNbr.Value,
            payload.PositionTypeCode);

        return [];
    }
}
