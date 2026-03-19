using CrewService.Domain.DomainEvents.UserAccess;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.UserAccess;
using CrewService.Persistance.Repositories;
using CrewService.UnitTests.Fixtures;

namespace CrewService.UnitTests.Modules.UserAccess;

public sealed class InvitationTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    /// <summary>
    /// Verifies that an invitation can be created, persisted, and retrieved by CtrlNbr.
    /// </summary>
    [Fact]
    public async Task Create_And_GetByCtrlNbr_Returns_Invitation()
    {
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new InvitationRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("Test Parent");
        await parentRepo.AddAsync(parent);

        var invitation = Invitation.Create("test@example.com", parent.CtrlNbr.Value, Roles.Dispatcher, "admin-001");
        await repo.AddAsync(invitation);

        var found = await repo.GetByCtrlNbrAsync(invitation.CtrlNbr);

        Assert.NotNull(found);
        Assert.Equal("test@example.com", found.Email);
        Assert.Equal(parent.CtrlNbr, found.ParentCtrlNbr);
        Assert.Equal(Roles.Dispatcher, found.Role);
        Assert.Equal(InvitationStatus.Pending, found.Status);
        Assert.NotEmpty(found.Token);
    }

    /// <summary>
    /// Verifies Create raises InvitationCreatedDomainEvent.
    /// </summary>
    [Fact]
    public void Create_Raises_CreatedDomainEvent()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Employee, "admin-001");

        Assert.Single(invitation.DomainEvents);
        Assert.IsType<InvitationCreatedDomainEvent>(invitation.DomainEvents[0]);
    }

    /// <summary>
    /// Verifies email is lowercased on creation.
    /// </summary>
    [Fact]
    public void Create_Normalizes_Email_To_Lowercase()
    {
        var invitation = Invitation.Create("Admin@Example.COM", 250101120000001, Roles.Employee, "admin-001");

        Assert.Equal("admin@example.com", invitation.Email);
    }

    /// <summary>
    /// Verifies Create throws for null/empty email, role, invitedByUserId.
    /// </summary>
    [Theory]
    [InlineData(null, Roles.Employee, "admin")]
    [InlineData("", Roles.Employee, "admin")]
    [InlineData("test@example.com", null, "admin")]
    [InlineData("test@example.com", "", "admin")]
    [InlineData("test@example.com", Roles.Employee, null)]
    [InlineData("test@example.com", Roles.Employee, "")]
    public void Create_With_Invalid_Arguments_Throws(string? email, string? role, string? invitedBy)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Invitation.Create(email!, 250101120000001, role!, invitedBy!));
    }

    /// <summary>
    /// Verifies that accepting a pending invitation transitions status and raises event.
    /// </summary>
    [Fact]
    public void Accept_Transitions_To_Accepted_And_Raises_Event()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Dispatcher, "admin-001");
        var initialCount = invitation.DomainEvents.Count;

        invitation.Accept();

        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.NotNull(invitation.AcceptedAt);
        Assert.Equal(initialCount + 1, invitation.DomainEvents.Count);
        Assert.IsType<InvitationAcceptedDomainEvent>(invitation.DomainEvents[^1]);
    }

    /// <summary>
    /// Verifies that accepting an already-accepted invitation throws.
    /// </summary>
    [Fact]
    public void Accept_Already_Accepted_Throws()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Dispatcher, "admin-001");
        invitation.Accept();

        Assert.Throws<InvalidOperationException>(() => invitation.Accept());
    }

    /// <summary>
    /// Verifies that revoking a pending invitation transitions status and raises event.
    /// </summary>
    [Fact]
    public void Revoke_Transitions_To_Revoked_And_Raises_Event()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Dispatcher, "admin-001");
        var initialCount = invitation.DomainEvents.Count;

        invitation.Revoke();

        Assert.Equal(InvitationStatus.Revoked, invitation.Status);
        Assert.Equal(initialCount + 1, invitation.DomainEvents.Count);
        Assert.IsType<InvitationRevokedDomainEvent>(invitation.DomainEvents[^1]);
    }

    /// <summary>
    /// Verifies that revoking an already-revoked invitation throws.
    /// </summary>
    [Fact]
    public void Revoke_Already_Revoked_Throws()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Dispatcher, "admin-001");
        invitation.Revoke();

        Assert.Throws<InvalidOperationException>(() => invitation.Revoke());
    }

    /// <summary>
    /// Verifies that revoking an accepted invitation throws.
    /// </summary>
    [Fact]
    public void Revoke_Accepted_Invitation_Throws()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Dispatcher, "admin-001");
        invitation.Accept();

        Assert.Throws<InvalidOperationException>(() => invitation.Revoke());
    }

    /// <summary>
    /// Verifies IsValid returns true for a pending, non-expired invitation.
    /// </summary>
    [Fact]
    public void IsValid_Returns_True_For_Pending_NonExpired()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Employee, "admin-001");

        Assert.True(invitation.IsValid);
    }

    /// <summary>
    /// Verifies IsValid returns false for an accepted invitation.
    /// </summary>
    [Fact]
    public void IsValid_Returns_False_For_Accepted()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Employee, "admin-001");
        invitation.Accept();

        Assert.False(invitation.IsValid);
    }

    /// <summary>
    /// Verifies GetByTokenAsync returns the matching invitation.
    /// </summary>
    [Fact]
    public async Task GetByToken_Returns_Matching_Invitation()
    {
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new InvitationRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("Token Parent");
        await parentRepo.AddAsync(parent);

        var invitation = Invitation.Create("token@example.com", parent.CtrlNbr.Value, Roles.Dispatcher, "admin-001");
        await repo.AddAsync(invitation);

        var found = await repo.GetByTokenAsync(invitation.Token);
        var notFound = await repo.GetByTokenAsync("nonexistent-token");

        Assert.NotNull(found);
        Assert.Equal(invitation.CtrlNbr, found.CtrlNbr);
        Assert.Null(notFound);
    }

    /// <summary>
    /// Verifies GetByEmailAsync returns all invitations for an email.
    /// </summary>
    [Fact]
    public async Task GetByEmail_Returns_All_For_Email()
    {
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new InvitationRepository(ctx, _factory.CurrentUserService);

        var parent1 = Parent.Create("Parent A");
        await parentRepo.AddAsync(parent1);
        var parent2 = Parent.Create("Parent B");
        await parentRepo.AddAsync(parent2);

        var i1 = Invitation.Create("multi@example.com", parent1.CtrlNbr.Value, Roles.Dispatcher, "admin-001");
        await repo.AddAsync(i1);
        var i2 = Invitation.Create("multi@example.com", parent2.CtrlNbr.Value, Roles.Employee, "admin-001");
        await repo.AddAsync(i2);
        var i3 = Invitation.Create("other@example.com", parent1.CtrlNbr.Value, Roles.Employee, "admin-001");
        await repo.AddAsync(i3);

        var results = await repo.GetByEmailAsync("multi@example.com");

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("multi@example.com", r.Email));
    }

    /// <summary>
    /// Verifies GetPendingByEmailAndParentAsync returns only pending invitations.
    /// </summary>
    [Fact]
    public async Task GetPendingByEmailAndParent_Returns_Only_Pending()
    {
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new InvitationRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("Pending Parent");
        await parentRepo.AddAsync(parent);

        var pending = Invitation.Create("pending@example.com", parent.CtrlNbr.Value, Roles.Dispatcher, "admin-001");
        await repo.AddAsync(pending);

        // Accept it, then create a new pending one
        pending.Accept();
        await repo.UpdateAsync(pending);

        var newPending = Invitation.Create("pending@example.com", parent.CtrlNbr.Value, Roles.CrewManager, "admin-001");
        await repo.AddAsync(newPending);

        var found = await repo.GetPendingByEmailAndParentAsync("pending@example.com", parent.CtrlNbr.Value);

        Assert.NotNull(found);
        Assert.Equal(Roles.CrewManager, found.Role);
        Assert.Equal(InvitationStatus.Pending, found.Status);
    }

    /// <summary>
    /// Verifies soft-delete excludes invitation from queries.
    /// </summary>
    [Fact]
    public async Task Remove_SoftDeletes_Invitation()
    {
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new InvitationRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("Delete Parent");
        await parentRepo.AddAsync(parent);

        var invitation = Invitation.Create("delete@example.com", parent.CtrlNbr.Value, Roles.Employee, "admin-001");
        await repo.AddAsync(invitation);

        await repo.DeleteAsync(invitation.CtrlNbr);

        var remaining = await repo.GetByEmailAsync("delete@example.com");
        Assert.Empty(remaining);
    }

    /// <summary>
    /// Verifies token uniqueness across invitations.
    /// </summary>
    [Fact]
    public void Generated_Tokens_Are_Unique()
    {
        var inv1 = Invitation.Create("a@example.com", 250101120000001, Roles.Employee, "admin-001");
        var inv2 = Invitation.Create("b@example.com", 250101120000001, Roles.Employee, "admin-001");

        Assert.NotEqual(inv1.Token, inv2.Token);
    }

    /// <summary>
    /// Verifies Create rejects an invalid role not in Roles.AllPerParentRoles.
    /// </summary>
    [Fact]
    public void Create_With_Invalid_Role_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Invitation.Create("test@example.com", 250101120000001, "FakeRole", "admin-001"));
    }

    /// <summary>
    /// Verifies Create rejects zero or negative expirationDays.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_With_Invalid_ExpirationDays_Throws(int days)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Invitation.Create("test@example.com", 250101120000001, Roles.Employee, "admin-001", days));
    }

    /// <summary>
    /// Verifies MarkExpired transitions a pending invitation to Expired.
    /// </summary>
    [Fact]
    public void MarkExpired_Transitions_Pending_To_Expired()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Dispatcher, "admin-001");

        invitation.MarkExpired();

        Assert.Equal(InvitationStatus.Expired, invitation.Status);
    }

    /// <summary>
    /// Verifies MarkExpired on a non-pending invitation throws.
    /// </summary>
    [Fact]
    public void MarkExpired_On_Accepted_Throws()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Dispatcher, "admin-001");
        invitation.Accept();

        Assert.Throws<InvalidOperationException>(() => invitation.MarkExpired());
    }

    /// <summary>
    /// Verifies Accept does NOT mutate status when it throws for expiration.
    /// The caller should explicitly call MarkExpired + persist.
    /// </summary>
    [Fact]
    public void Accept_On_Expired_Does_Not_Mutate_Status()
    {
        // Create with minimal expiration then manually mark expired to simulate
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Dispatcher, "admin-001");
        invitation.MarkExpired();

        // Now it's Expired � Accept should throw because status is not Pending
        Assert.Throws<InvalidOperationException>(() => invitation.Accept());
        Assert.Equal(InvitationStatus.Expired, invitation.Status);
    }

    /// <summary>
    /// Verifies accepting a revoked invitation throws.
    /// </summary>
    [Fact]
    public void Accept_Revoked_Invitation_Throws()
    {
        var invitation = Invitation.Create("test@example.com", 250101120000001, Roles.Dispatcher, "admin-001");
        invitation.Revoke();

        Assert.Throws<InvalidOperationException>(() => invitation.Accept());
    }
}
