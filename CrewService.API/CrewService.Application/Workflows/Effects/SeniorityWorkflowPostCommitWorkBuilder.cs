using CrewService.Domain.Modules.Staffing;

namespace CrewService.Application.Workflows.Effects;

public static class SeniorityWorkflowPostCommitWorkBuilder
{
    public static IReadOnlyList<WorkflowEffectPostCommitWorkItem> BuildVacancyRepostWorkItems(
        IReadOnlyList<VacatedAssignmentResult> results)
    {
        if (results.Count == 0)
            return [];

        var workItems = new List<WorkflowEffectPostCommitWorkItem>();
        foreach (var result in results)
        {
            if (result.PositionType == StaffablePositionType.Crew)
            {
                workItems.Add(new WorkflowEffectPostCommitWorkItem(
                    WorkflowPostCommitWorkTypes.RepostVacatedPosition,
                    new RepostVacatedPositionPostCommitPayload(
                        result.VacatedStaffablePositionCtrlNbr,
                        result.PreviousIncumbentCtrlNbr)));
                continue;
            }

            if (result.IsExtraBoard && result.BoardCtrlNbr is not null)
            {
                workItems.Add(new WorkflowEffectPostCommitWorkItem(
                    WorkflowPostCommitWorkTypes.RepostBoardPositionIfUnderstaffed,
                    new RepostBoardPositionIfUnderstaffedPostCommitPayload(
                        result.BoardCtrlNbr,
                        result.VacatedStaffablePositionCtrlNbr,
                        result.PreviousIncumbentCtrlNbr)));
            }
        }

        return workItems;
    }
}