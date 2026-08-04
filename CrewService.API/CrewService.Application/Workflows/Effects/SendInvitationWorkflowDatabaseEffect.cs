using CrewService.Application.UserAccess;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Workflows.Effects;

public sealed class SendInvitationWorkflowDatabaseEffect(InvitationAppService invitationAppService) : IDatabaseWorkflowEffect
{
    public string EffectTypeCode => WorkflowEffectTypeCodes.SendInvitation;

    public async Task<IReadOnlyList<WorkflowEffectPostCommitWorkItem>> ExecuteAsync(WorkflowEffectExecutionContext context)
    {
        var effect = context.Effect;
        var ct = context.CancellationToken;
        var runtime = context.RuntimeContext;

        if (!effect.Options.TryGetValue(WorkflowOptionKeys.RoleCtrlNbr, out var roleCtrlNbrRaw)
            || !long.TryParse(roleCtrlNbrRaw, out var roleCtrlNbrValue)
            || roleCtrlNbrValue <= 0)
        {
            throw new InvalidOperationException("Send Invitation effect requires a valid roleCtrlNbr option.");
        }

        var expirationDays = 7;
        if (effect.Options.TryGetValue(WorkflowOptionKeys.ExpirationDays, out var expirationDaysRaw)
            && int.TryParse(expirationDaysRaw, out var parsedExpirationDays)
            && parsedExpirationDays > 0)
        {
            expirationDays = parsedExpirationDays;
        }

        var usePrimaryEmail = true;
        if (effect.Options.TryGetValue(WorkflowOptionKeys.UsePrimaryEmail, out var usePrimaryEmailRaw)
            && bool.TryParse(usePrimaryEmailRaw, out var parsedUsePrimaryEmail))
        {
            usePrimaryEmail = parsedUsePrimaryEmail;
        }

        var effectRailroadCtrlNbr = runtime.TriggerRailroadCtrlNbr;
        if (effect.Options.TryGetValue(WorkflowOptionKeys.RailroadCtrlNbr, out var railroadCtrlNbrRaw)
            && long.TryParse(railroadCtrlNbrRaw, out var railroadCtrlNbrValue)
            && railroadCtrlNbrValue > 0)
        {
            effectRailroadCtrlNbr = ControlNumber.Create(railroadCtrlNbrValue);
        }

        if (string.IsNullOrWhiteSpace(runtime.InvitedByUserId) || string.IsNullOrWhiteSpace(runtime.InvitedByUserName))
            throw new InvalidOperationException("Employee Created trigger payload is missing invited-by audit fields.");

        var role = await context.Uow.Roles.GetByCtrlNbrAsync(ControlNumber.Create(roleCtrlNbrValue), ct)
            ?? throw new InvalidOperationException($"Role {roleCtrlNbrValue} not found for Send Invitation effect.");

        var parentCtrlNbr = runtime.ClientCtrlNbr > 0 ? ControlNumber.Create(runtime.ClientCtrlNbr) : null;
        var parentName = parentCtrlNbr is null
            ? "CrewService"
            : (await context.Uow.Parents.GetByCtrlNbrAsync(parentCtrlNbr, ct))?.Name.Value ?? $"Parent {parentCtrlNbr.Value}";

        var invitationEmail = usePrimaryEmail ? runtime.PrimaryEmail : runtime.TriggerEmail;
        if (string.IsNullOrWhiteSpace(invitationEmail))
            throw new InvalidOperationException(
                usePrimaryEmail
                    ? "Send Invitation effect requires employee primary email, but no primary email is available."
                    : "Send Invitation effect requires trigger email, but no trigger email is available.");

        var invitation = await invitationAppService.CreateFromSystemInOrchestrationAsync(
            context.Uow,
            invitationEmail,
            parentCtrlNbr,
            role.Name,
            runtime.InvitedByUserId,
            runtime.InvitedByUserName,
            expirationDays,
            effectRailroadCtrlNbr,
            ct);

        if (invitation is null)
            return [];

        return
        [
            new WorkflowEffectPostCommitWorkItem(
                WorkflowPostCommitWorkTypes.SendInvitationEmail,
                new SendInvitationPostCommitPayload(invitation, parentName))
        ];
    }
}
