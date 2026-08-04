using CrewService.Application.UserAccess;
using CrewService.Application.VacancyAssignment;

namespace CrewService.Application.Workflows.Effects;

public sealed class WorkflowPostCommitDispatcher(
    InvitationAppService invitationAppService,
    IVacancyRepostService vacancyRepostService) : IWorkflowPostCommitDispatcher
{
    public async Task DispatchAsync(IReadOnlyList<WorkflowEffectPostCommitWorkItem> workItems, CancellationToken ct = default)
    {
        foreach (var workItem in workItems)
        {
            switch (workItem.WorkType)
            {
                case WorkflowPostCommitWorkTypes.SendInvitationEmail:
                {
                    if (workItem.Payload is not SendInvitationPostCommitPayload payload)
                        throw new InvalidOperationException("Invalid post-commit payload for SendInvitationEmail work item.");

                    await invitationAppService.SendInvitationEmailAsync(payload.Invitation, payload.ParentName);
                    break;
                }

                case WorkflowPostCommitWorkTypes.RepostVacatedPosition:
                {
                    if (workItem.Payload is not RepostVacatedPositionPostCommitPayload payload)
                        throw new InvalidOperationException("Invalid post-commit payload for RepostVacatedPosition work item.");

                    await vacancyRepostService.RepostVacatedPositionAsync(
                        payload.StaffablePositionCtrlNbr,
                        payload.PreviousIncumbentCtrlNbr,
                        ct);
                    break;
                }

                case WorkflowPostCommitWorkTypes.RepostBoardPositionIfUnderstaffed:
                {
                    if (workItem.Payload is not RepostBoardPositionIfUnderstaffedPostCommitPayload payload)
                    {
                        throw new InvalidOperationException(
                            "Invalid post-commit payload for RepostBoardPositionIfUnderstaffed work item.");
                    }

                    await vacancyRepostService.RepostBoardPositionIfUnderstaffedAsync(
                        payload.BoardCtrlNbr,
                        payload.VacatedStaffablePositionCtrlNbr,
                        payload.PreviousIncumbentCtrlNbr,
                        ct);
                    break;
                }

                default:
                    throw new InvalidOperationException($"Unsupported workflow post-commit work type '{workItem.WorkType}'.");
            }
        }
    }
}
