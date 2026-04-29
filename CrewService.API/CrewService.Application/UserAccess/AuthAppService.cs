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
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var invitation = await uow.Invitations.GetByTokenAsync(token);
        if (invitation is null)
            return (false, "Invalid invitation token.");

        currentUserService.SetAuditOverride(invitation.Email);

        if (!invitation.IsValid)
        {
            if (invitation.Status == InvitationStatus.Pending && DateTime.UtcNow > invitation.ExpiresAt)
            {
                invitation.MarkExpired();
                await uow.Invitations.UpdateAsync(invitation);
                await uow.CommitAsync(ct);
            }
            return (false, $"Invitation is no longer valid (status: {invitation.Status}).");
        }

        var existingUser = await userAccountService.FindByEmailAsync(invitation.Email);
        var needsPassword = existingUser is null || !await userAccountService.HasPasswordAsync(existingUser.Id);

        if (needsPassword)
        {
            if (string.IsNullOrEmpty(password))
                return (false, "Password is required.");

            if (existingUser is null)
            {
                // New user (e.g. admin invitation) -- create account with password
                var createResult = await userAccountService.CreateAsync(new CreateUserRequest
                {
                    UserName = invitation.Email,
                    Email = invitation.Email,
                    Password = password
                });

                if (!createResult.Result.Succeeded)
                    return (false, string.Join("; ", createResult.Result.Errors));

                existingUser = await userAccountService.FindByIdAsync(createResult.UserId);
            }
            else
            {
                // Pre-created employee account -- set the password now
                var setResult = await userAccountService.SetPasswordAsync(existingUser.Id, password);
                if (!setResult.Succeeded)
                    return (false, string.Join("; ", setResult.Errors));
            }
        }

        invitation.Accept();
        await uow.Invitations.UpdateAsync(invitation);

        if (invitation.ParentCtrlNbr is null)
        {
            await userAccountService.UpdatePrimaryRoleAsync(existingUser!.Id, Roles.SystemAdmin);

            var oldInvitations = await uow.Invitations.GetAcceptedByEmailAndParentAsync(invitation.Email, null);
            foreach (var oldInv in oldInvitations.Where(i => i.CtrlNbr != invitation.CtrlNbr))
            {
                oldInv.MarkSuperseded();
                await uow.Invitations.UpdateAsync(oldInv);
            }

            await uow.CommitAsync(ct);
            return (true, null);
        }

        var existingAssignments = await uow.UserParentAssignments.GetByUserAndParentAsync(
            existingUser!.Id, invitation.ParentCtrlNbr!);
        var isParentScoped = !Roles.RequiresRailroad(invitation.Role);

        if (existingAssignments.Count > 0)
        {
            var hasRailroadScoped = existingAssignments.Any(a => Roles.RequiresRailroad(a.Role));
            var hasParentScoped = existingAssignments.Any(a => !Roles.RequiresRailroad(a.Role));

            if (isParentScoped && hasRailroadScoped)
            {
                foreach (var old in existingAssignments)
                    await uow.UserParentAssignments.DeleteAsync(old.CtrlNbr);

                var newAssignment = UserParentAssignment.Create(
                    existingUser.Id, invitation.ParentCtrlNbr, invitation.Role);
                await uow.UserParentAssignments.AddAsync(newAssignment);
            }
            else if (!isParentScoped && hasParentScoped)
            {
                foreach (var old in existingAssignments)
                    await uow.UserParentAssignments.DeleteAsync(old.CtrlNbr);

                var newAssignment = UserParentAssignment.Create(
                    existingUser.Id, invitation.ParentCtrlNbr, invitation.Role, invitation.RailroadCtrlNbr);
                await uow.UserParentAssignments.AddAsync(newAssignment);
            }
            else
            {
                var matchingAssignment = existingAssignments
                    .FirstOrDefault(a => a.RailroadCtrlNbr == invitation.RailroadCtrlNbr);
                if (matchingAssignment is not null)
                {
                    matchingAssignment.UpdateRole(invitation.Role, invitation.RailroadCtrlNbr);
                    await uow.UserParentAssignments.UpdateAsync(matchingAssignment);
                }
                else
                {
                    var newAssignment = UserParentAssignment.Create(
                        existingUser.Id, invitation.ParentCtrlNbr, invitation.Role, invitation.RailroadCtrlNbr);
                    await uow.UserParentAssignments.AddAsync(newAssignment);
                }
            }

            var oldInvitations = await uow.Invitations.GetAcceptedByEmailAndParentAsync(
                invitation.Email, invitation.ParentCtrlNbr);
            foreach (var oldInv in oldInvitations.Where(i => i.CtrlNbr != invitation.CtrlNbr))
            {
                oldInv.MarkSuperseded();
                await uow.Invitations.UpdateAsync(oldInv);
            }
        }
        else
        {
            var assignment = UserParentAssignment.Create(
                existingUser.Id, invitation.ParentCtrlNbr, invitation.Role, invitation.RailroadCtrlNbr);
            await uow.UserParentAssignments.AddAsync(assignment);
        }

        await uow.CommitAsync(ct);
        return (true, null);
    }
}
