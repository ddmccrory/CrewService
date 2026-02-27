using CrewService.Domain.DomainEvents.UserAccess;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.UserAccess;

public sealed class Invitation : Entity
{
    public string Email { get; private set; } = string.Empty;
    public ControlNumber ParentCtrlNbr { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string InvitedByUserId { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public InvitationStatus Status { get; private set; }

    private Invitation()
    {
        ParentCtrlNbr = null!;
    }

    private Invitation(
        string email,
        ControlNumber parentCtrlNbr,
        string role,
        string invitedByUserId,
        string token,
        DateTime expiresAt)
    {
        Email = email;
        ParentCtrlNbr = parentCtrlNbr;
        Role = role;
        InvitedByUserId = invitedByUserId;
        Token = token;
        ExpiresAt = expiresAt;
        Status = InvitationStatus.Pending;
    }

    public static Invitation Create(
        string email,
        long parentCtrlNbr,
        string role,
        string invitedByUserId,
        int expirationDays = 7)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        ArgumentException.ThrowIfNullOrEmpty(role);
        ArgumentException.ThrowIfNullOrEmpty(invitedByUserId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expirationDays);

        if (!Roles.AllPerParentRoles.Contains(role))
            throw new ArgumentException($"Unknown role '{role}'.", nameof(role));

        var invitation = new Invitation(
            email.ToLowerInvariant(),
            ControlNumber.Create(parentCtrlNbr),
            role,
            invitedByUserId,
            GenerateToken(),
            DateTime.UtcNow.AddDays(expirationDays));

        invitation.Raise(new InvitationCreatedDomainEvent(invitation.CtrlNbr));

        return invitation;
    }

    public void Accept()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot accept invitation with status '{Status}'.");

        if (DateTime.UtcNow > ExpiresAt)
            throw new InvalidOperationException("Invitation has expired.");

        Status = InvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        Raise(new InvitationAcceptedDomainEvent(CtrlNbr));
    }

    /// <summary>
    /// Explicitly marks a pending invitation as expired. Should be called
    /// when expiration is detected so the status is persisted.
    /// </summary>
    public void MarkExpired()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot expire invitation with status '{Status}'.");

        Status = InvitationStatus.Expired;
    }

    public void Revoke()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot revoke invitation with status '{Status}'.");

        Status = InvitationStatus.Revoked;
        Raise(new InvitationRevokedDomainEvent(CtrlNbr));
    }

    public bool IsValid => Status == InvitationStatus.Pending && DateTime.UtcNow <= ExpiresAt;

    private static string GenerateToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }
}
