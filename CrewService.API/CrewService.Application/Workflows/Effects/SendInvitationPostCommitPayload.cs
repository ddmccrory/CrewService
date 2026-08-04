using CrewService.Domain.Models.UserAccess;

namespace CrewService.Application.Workflows.Effects;

public sealed record SendInvitationPostCommitPayload(
    Invitation Invitation,
    string ParentName);
