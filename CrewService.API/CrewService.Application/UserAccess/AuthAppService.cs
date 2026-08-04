using CrewService.Application.Models.UserAccount;
using CrewService.Application.Modules.UserAccount;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.UserAccess;

public sealed class AuthAppService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    IUserAccountService userAccountService,
    ICurrentUserService currentUserService)
{
    public async Task<(bool Success, string? ErrorMessage)> AcceptInvitationAsync(
        string token, string? password, CancellationToken ct = default)
    {
        string invitationEmail;
        ControlNumber? invitationParentCtrlNbr;
        string invitationRole;
        ControlNumber? invitationRailroadCtrlNbr;

        await using (var invitationLookupUow = await uowFactory.CreateAsync(cancellationToken: ct))
        {
            var invitation = await invitationLookupUow.Invitations.GetByTokenAsync(token);
            if (invitation is null)
                return (false, "Invalid invitation token.");

            currentUserService.SetAuditOverride(invitation.Email);

            if (!invitation.IsValid)
            {
                if (invitation.Status == InvitationStatus.Pending && DateTime.UtcNow > invitation.ExpiresAt)
                {
                    invitation.MarkExpired();
                    await invitationLookupUow.Invitations.UpdateAsync(invitation, ct);
                    await invitationLookupUow.CommitAsync(ct);
                }

                return (false, $"Invitation is no longer valid (status: {invitation.Status}).");
            }

            invitationEmail = invitation.Email;
            invitationParentCtrlNbr = invitation.ParentCtrlNbr;
            invitationRole = invitation.Role;
            invitationRailroadCtrlNbr = invitation.RailroadCtrlNbr;
        }

        var existingUser = await userAccountService.FindByEmailAsync(invitationEmail);
        var needsPassword = existingUser is null || !await userAccountService.HasPasswordAsync(existingUser.Id);

        if (needsPassword)
        {
            if (string.IsNullOrEmpty(password))
                return (false, "Password is required.");

            if (existingUser is null)
            {
                var (createUserResult, userId) = await userAccountService.CreateAsync(new CreateUserRequest
                {
                    UserName = invitationEmail,
                    Email = invitationEmail,
                    Password = password
                });

                if (!createUserResult.Succeeded)
                    return (false, string.Join("; ", createUserResult.Errors));

                existingUser = await userAccountService.FindByIdAsync(userId);
                if (existingUser is null)
                    return (false, "User account could not be loaded after creation.");
            }
            else
            {
                var setResult = await userAccountService.SetPasswordAsync(existingUser.Id, password);
                if (!setResult.Succeeded)
                    return (false, string.Join("; ", setResult.Errors));
            }
        }

        if (invitationParentCtrlNbr is null)
        {
            var updateRoleResult = await userAccountService.UpdatePrimaryRoleAsync(existingUser!.Id, Roles.SystemAdmin);
            if (!updateRoleResult.Succeeded)
                return (false, string.Join("; ", updateRoleResult.Errors));
        }

        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var finalInvitation = await uow.Invitations.GetByTokenAsync(token);
        if (finalInvitation is null)
            return (false, "Invalid invitation token.");

        currentUserService.SetAuditOverride(finalInvitation.Email);

        if (!finalInvitation.IsValid)
        {
            if (finalInvitation.Status == InvitationStatus.Pending && DateTime.UtcNow > finalInvitation.ExpiresAt)
            {
                finalInvitation.MarkExpired();
                await uow.Invitations.UpdateAsync(finalInvitation, ct);
                await uow.CommitAsync(ct);
            }

            return (false, $"Invitation is no longer valid (status: {finalInvitation.Status}).");
        }

        finalInvitation.Accept();
        await uow.Invitations.UpdateAsync(finalInvitation, ct);

        if (invitationParentCtrlNbr is null)
        {
            var oldInvitations = await uow.Invitations.GetAcceptedByEmailAndParentAsync(invitationEmail, null);
            foreach (var oldInv in oldInvitations.Where(i => i.CtrlNbr != finalInvitation.CtrlNbr))
            {
                oldInv.MarkSuperseded();
                await uow.Invitations.UpdateAsync(oldInv, ct);
            }

            await uow.CommitAsync(ct);
            return (true, null);
        }

        var existingAssignments = await uow.UserParentAssignments.GetByUserAndParentAsync(
            existingUser!.Id, invitationParentCtrlNbr);
        var isParentScoped = !Roles.RequiresRailroad(invitationRole);

        if (existingAssignments.Count > 0)
        {
            var hasRailroadScoped = existingAssignments.Any(a => Roles.RequiresRailroad(a.Role));
            var hasParentScoped = existingAssignments.Any(a => !Roles.RequiresRailroad(a.Role));

            if (isParentScoped && hasRailroadScoped)
            {
                foreach (var old in existingAssignments)
                    await uow.UserParentAssignments.DeleteAsync(old.CtrlNbr, ct);

                var newAssignment = UserParentAssignment.Create(existingUser.Id, invitationParentCtrlNbr, invitationRole);
                await uow.UserParentAssignments.AddAsync(newAssignment, ct);
            }
            else if (!isParentScoped && hasParentScoped)
            {
                foreach (var old in existingAssignments)
                    await uow.UserParentAssignments.DeleteAsync(old.CtrlNbr, ct);

                var newAssignment = UserParentAssignment.Create(existingUser.Id, invitationParentCtrlNbr, invitationRole, invitationRailroadCtrlNbr);
                await uow.UserParentAssignments.AddAsync(newAssignment, ct);
            }
            else
            {
                var matchingAssignment = existingAssignments
                    .FirstOrDefault(a => a.RailroadCtrlNbr == invitationRailroadCtrlNbr);
                if (matchingAssignment is not null)
                {
                    matchingAssignment.UpdateRole(invitationRole, invitationRailroadCtrlNbr);
                    await uow.UserParentAssignments.UpdateAsync(matchingAssignment, ct);
                }
                else
                {
                    var newAssignment = UserParentAssignment.Create(existingUser.Id, invitationParentCtrlNbr, invitationRole, invitationRailroadCtrlNbr);
                    await uow.UserParentAssignments.AddAsync(newAssignment, ct);
                }
            }

            var oldAcceptedInvitations = await uow.Invitations.GetAcceptedByEmailAndParentAsync(invitationEmail, invitationParentCtrlNbr);
            foreach (var oldInv in oldAcceptedInvitations.Where(i => i.CtrlNbr != finalInvitation.CtrlNbr))
            {
                oldInv.MarkSuperseded();
                await uow.Invitations.UpdateAsync(oldInv, ct);
            }
        }
        else
        {
            var assignment = UserParentAssignment.Create(existingUser.Id, invitationParentCtrlNbr, invitationRole, invitationRailroadCtrlNbr);
            await uow.UserParentAssignments.AddAsync(assignment, ct);
        }

        await uow.CommitAsync(ct);
        return (true, null);
    }
}
