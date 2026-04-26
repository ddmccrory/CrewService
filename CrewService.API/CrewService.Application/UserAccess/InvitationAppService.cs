using CrewService.Application.Modules.UserAccess;
using CrewService.Application.Modules.UserAccount;
using CrewService.Domain.Exceptions;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace CrewService.Application.UserAccess;

public sealed class InvitationAppService(
    IOrchestrationUnitOfWorkFactory uowFactory,
    ICurrentUserService currentUserService,
    IInvitationEmailService emailService,
    IConfiguration configuration)
{
    private readonly string _baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://localhost:7132";

    public async Task<Invitation> GetAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Invitations.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Invitation {ctrlNbr.Value} not found.");
    }

    public async Task<List<Invitation>> GetByParentAsync(long parentCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Invitations.GetByParentCtrlNbrAsync(ControlNumber.Create(parentCtrlNbr));
    }

    public async Task<List<Invitation>> GetByRoleAsync(string role, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Invitations.GetByRoleAsync(role);
    }

    public async Task<List<Invitation>> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.Invitations.GetByEmailAsync(email);
    }

    public async Task<Invitation> CreateAsync(
        string email, ControlNumber? parentCtrlNbr, string role,
        ControlNumber? railroadCtrlNbr, int expirationDays, string parentName,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);

        var existing = await uow.Invitations.GetPendingByEmailAndParentAsync(email, parentCtrlNbr);
        if (existing is not null)
            throw new ConflictException(nameof(Invitation), $"A pending invitation already exists for {email}.");

        var invitation = Invitation.Create(
            email, parentCtrlNbr, role,
            currentUserService.GetUserId().ToString(),
            expirationDays, railroadCtrlNbr);

        await uow.Invitations.AddAsync(invitation);
        await uow.CommitAsync(ct);

        var acceptUrl = $"{_baseUrl}/Account/AcceptInvitation?token={Uri.EscapeDataString(invitation.Token)}";
        await emailService.SendInvitationAsync(invitation.Email, invitation.Role, parentName, acceptUrl, invitation.ExpiresAt);

        return invitation;
    }

    public async Task<Invitation> RevokeAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var invitation = await uow.Invitations.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Invitation {ctrlNbr.Value} not found.");
        invitation.Revoke();
        await uow.Invitations.UpdateAsync(invitation);
        await uow.CommitAsync(ct);
        return invitation;
    }

    public async Task<Invitation> ResendAsync(ControlNumber ctrlNbr, string parentName, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.Invitations.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Invitation {ctrlNbr.Value} not found.");

        if (existing.Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot resend invitation with status '{existing.Status}'.");

        existing.Revoke();
        await uow.Invitations.UpdateAsync(existing);

        var newInvitation = Invitation.Create(
            existing.Email, existing.ParentCtrlNbr, existing.Role,
            currentUserService.GetUserId().ToString(),
            railroadCtrlNbr: existing.RailroadCtrlNbr);

        await uow.Invitations.AddAsync(newInvitation);
        await uow.CommitAsync(ct);

        var acceptUrl = $"{_baseUrl}/Account/AcceptInvitation?token={Uri.EscapeDataString(newInvitation.Token)}";
        await emailService.SendReminderAsync(newInvitation.Email, newInvitation.Role, parentName, acceptUrl, newInvitation.ExpiresAt);

        return newInvitation;
    }

    public async Task<(bool IsValid, string Email, string Role, string Status, bool UserAlreadyExists, string RailroadName, ControlNumber? ParentCtrlNbr)>
        ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var invitation = await uow.Invitations.GetByTokenAsync(token);
        if (invitation is null)
            return (false, string.Empty, string.Empty, string.Empty, false, string.Empty, null);

        if (invitation.Status == InvitationStatus.Pending && DateTime.UtcNow > invitation.ExpiresAt)
        {
            invitation.MarkExpired();
            await uow.Invitations.UpdateAsync(invitation);
            await uow.CommitAsync(ct);
        }

        return (invitation.IsValid, invitation.Email, invitation.Role, invitation.Status.ToString(), false, string.Empty, invitation.ParentCtrlNbr);
    }

    public async Task<string> GetParentNameAsync(ControlNumber? parentCtrlNbr, CancellationToken ct = default)
    {
        if (parentCtrlNbr is null) return "CrewService";
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var parent = await uow.Parents.GetByCtrlNbrAsync(parentCtrlNbr);
        return parent?.Name.Value ?? $"Parent {parentCtrlNbr.Value}";
    }

    public async Task<string> GetRailroadNameAsync(
        ControlNumber? parentCtrlNbr, ControlNumber? railroadCtrlNbr, CancellationToken ct = default)
    {
        if (parentCtrlNbr is null || railroadCtrlNbr is null) return string.Empty;
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var railroads = await uow.DynamicGroups.GetByGroupTypeNameAsync("Railroad", parentCtrlNbr);
        return railroads.FirstOrDefault(rr => rr.CtrlNbr == railroadCtrlNbr)?.Name ?? string.Empty;
    }

    public async Task<bool> ValidateRailroadBelongsToParentAsync(
        long parentCtrlNbr, long railroadCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var railroads = await uow.DynamicGroups.GetByGroupTypeNameAsync("Railroad", ControlNumber.Create(parentCtrlNbr));
        return railroads.Any(rr => rr.CtrlNbr.Value == railroadCtrlNbr);
    }
}
