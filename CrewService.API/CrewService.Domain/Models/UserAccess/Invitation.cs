using CrewService.Domain.DomainEvents.UserAccess;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Models.UserAccess;

public sealed class Invitation : Entity
{
    public string Email { get; private set; } = string.Empty;
    public ControlNumber? ParentCtrlNbr { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string InvitedByUserId { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime? SupersededAt { get; private set; }
    public ControlNumber? RailroadCtrlNbr { get; private set; }
    public InvitationStatus Status { get; private set; }

    private Invitation()
    {
    }

    private Invitation(
        string email,
        ControlNumber? parentCtrlNbr,
        string role,
        string invitedByUserId,
        string token,
        DateTime expiresAt,
        ControlNumber? railroadCtrlNbr = null)
    {
        Email = email;
        ParentCtrlNbr = parentCtrlNbr;
        Role = role;
        InvitedByUserId = invitedByUserId;
        Token = token;
        ExpiresAt = expiresAt;
        RailroadCtrlNbr = railroadCtrlNbr;
        Status = InvitationStatus.Pending;
    }

    public static Invitation Create(
        string email,
        ControlNumber? parentCtrlNbr,
        string role,
        string invitedByUserId,
        int expirationDays = 7,
        ControlNumber? railroadCtrlNbr = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        ArgumentException.ThrowIfNullOrEmpty(role);
        ArgumentException.ThrowIfNullOrEmpty(invitedByUserId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expirationDays);

        if (!Roles.AllInvitableRoles.Contains(role))
            throw new ArgumentException($"Unknown role '{role}'.", nameof(role));

        var invitation = new Invitation(
            email.ToLowerInvariant(),
            parentCtrlNbr,
            role,
            invitedByUserId,
            GenerateToken(),
            DateTime.UtcNow.AddDays(expirationDays),
            railroadCtrlNbr);

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
        RevokedAt = DateTime.UtcNow;
        Raise(new InvitationRevokedDomainEvent(CtrlNbr));
    }

    public void MarkSuperseded()
    {
        if (Status != InvitationStatus.Accepted)
            throw new InvalidOperationException($"Cannot supersede invitation with status '{Status}'.");

        Status = InvitationStatus.Superseded;
        SupersededAt = DateTime.UtcNow;
    }

    public bool IsValid => Status == InvitationStatus.Pending && DateTime.UtcNow <= ExpiresAt;

    private static string GenerateToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }
}
