namespace CrewService.Application.Workflows.Effects;

public static class WorkflowPostCommitWorkTypes
{
    public const string SendInvitationEmail = "SendInvitationEmail";
    public const string RepostVacatedPosition = "RepostVacatedPosition";
    public const string RepostBoardPositionIfUnderstaffed = "RepostBoardPositionIfUnderstaffed";
}
