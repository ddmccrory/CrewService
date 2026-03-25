using CrewService.Domain.DomainEvents;
using CrewService.Domain.DomainEvents.UserAccess;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Repositories;
using CrewService.Persistance.Modules.TenantConfig;
using CrewService.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace CrewService.UnitTests.Modules.UserAccess;

public sealed class UserParentAssignmentTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    /// <summary>
    /// Verifies that a UserParentAssignment can be created, persisted, and retrieved by CtrlNbr.
    /// </summary>
    [Fact]
    public async Task Create_And_GetByCtrlNbr_Returns_Assignment()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new UserParentAssignmentRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("Test Parent");
        await parentRepo.AddAsync(parent, TestContext.Current.CancellationToken);

        var assignment = UserParentAssignment.Create("user-001", parent.CtrlNbr.Value, Roles.ParentAdmin);
        await repo.AddAsync(assignment, TestContext.Current.CancellationToken);

        // Act
        var found = await repo.GetByCtrlNbrAsync(assignment.CtrlNbr, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("user-001", found.UserId);
        Assert.Equal(parent.CtrlNbr, found.ParentCtrlNbr);
        Assert.Equal(Roles.ParentAdmin, found.Role);
    }

    /// <summary>
    /// Verifies domain event is raised on creation.
    /// </summary>
    [Fact]
    public void Create_Raises_CreatedDomainEvent()
    {
        var assignment = UserParentAssignment.Create("user-001", 250101120000001, "Dispatcher");

        Assert.Single(assignment.DomainEvents);
        Assert.IsType<CrewService.Domain.DomainEvents.UserAccess.UserParentAssignmentCreatedDomainEvent>(assignment.DomainEvents[0]);
    }

    /// <summary>
    /// Verifies GetByUserIdAsync returns all assignments for a given user.
    /// </summary>
    [Fact]
    public async Task GetByUserId_Returns_All_Assignments_For_User()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new UserParentAssignmentRepository(ctx, _factory.CurrentUserService);

        var parent1 = Parent.Create("Parent A");
        await parentRepo.AddAsync(parent1, TestContext.Current.CancellationToken);

        var parent2 = Parent.Create("Parent B");
        await parentRepo.AddAsync(parent2, TestContext.Current.CancellationToken);

        var a1 = UserParentAssignment.Create("user-multi", parent1.CtrlNbr.Value, Roles.ParentAdmin);
        await repo.AddAsync(a1, TestContext.Current.CancellationToken);

        var a2 = UserParentAssignment.Create("user-multi", parent2.CtrlNbr.Value, Roles.Employee);
        await repo.AddAsync(a2, TestContext.Current.CancellationToken);

        // A different user's assignment — should not be returned
        var a3 = UserParentAssignment.Create("user-other", parent1.CtrlNbr.Value, "CrewManager");
        await repo.AddAsync(a3, TestContext.Current.CancellationToken);

        // Act
        var results = await repo.GetByUserIdAsync("user-multi");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("user-multi", r.UserId));
    }

    /// <summary>
    /// Verifies GetByParentCtrlNbrAsync returns all assignments for a given parent.
    /// </summary>
    [Fact]
    public async Task GetByParentCtrlNbr_Returns_All_Assignments_For_Parent()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new UserParentAssignmentRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("Shared Parent");
        await parentRepo.AddAsync(parent, TestContext.Current.CancellationToken);

        var a1 = UserParentAssignment.Create("user-x", parent.CtrlNbr.Value, Roles.ParentAdmin);
        await repo.AddAsync(a1, TestContext.Current.CancellationToken);

        var a2 = UserParentAssignment.Create("user-y", parent.CtrlNbr.Value, "Dispatcher");
        await repo.AddAsync(a2, TestContext.Current.CancellationToken);

        // Act
        var results = await repo.GetByParentCtrlNbrAsync(parent.CtrlNbr.Value);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.UserId == "user-x");
        Assert.Contains(results, r => r.UserId == "user-y");
    }

    /// <summary>
    /// Verifies GetByUserAndParentAsync returns the exact match or null.
    /// </summary>
    [Fact]
    public async Task GetByUserAndParent_Returns_Exact_Match_Or_Null()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new UserParentAssignmentRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("Exact Match Parent");
        await parentRepo.AddAsync(parent, TestContext.Current.CancellationToken);

        var assignment = UserParentAssignment.Create("user-exact", parent.CtrlNbr.Value, "CrewManager");
        await repo.AddAsync(assignment, TestContext.Current.CancellationToken);

        // Act
        var found = await repo.GetByUserAndParentAsync("user-exact", parent.CtrlNbr.Value);
        var notFound = await repo.GetByUserAndParentAsync("user-nonexistent", parent.CtrlNbr.Value);

        // Assert
        Assert.Single(found);
        Assert.Equal(assignment.CtrlNbr, found[0].CtrlNbr);
        Assert.Empty(notFound);
    }

    /// <summary>
    /// Verifies that inserting a duplicate (UserId, ParentCtrlNbr, RailroadCtrlNbr) violates the unique index.
    /// </summary>
    [Fact]
    public async Task Duplicate_UserId_ParentCtrlNbr_RailroadCtrlNbr_Throws()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new UserParentAssignmentRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("Dup Parent");
        await parentRepo.AddAsync(parent, TestContext.Current.CancellationToken);

        var groupTypeRepo = new GroupTypeRepository(ctx, _factory.CurrentUserService);
        var groupType = GroupType.Create("Railroad", "Test RR GroupType", false);
        await groupTypeRepo.AddAsync(groupType, TestContext.Current.CancellationToken);

        var groupRepo = new DynamicGroupRepository(ctx, _factory.CurrentUserService);
        var railroad = DynamicGroup.Create(groupType.CtrlNbr, "Test RR", null, null, false, "TSTRR");
        await groupRepo.AddAsync(railroad, TestContext.Current.CancellationToken);

        var a1 = UserParentAssignment.Create("user-dup", parent.CtrlNbr.Value, Roles.RailroadAdmin, railroad.CtrlNbr);
        await repo.AddAsync(a1, TestContext.Current.CancellationToken);

        // Same user, same parent, same railroad = unique index violation
        var a2 = UserParentAssignment.Create("user-dup", parent.CtrlNbr.Value, Roles.Employee, railroad.CtrlNbr);

        // Act & Assert � unique index violation
        await Assert.ThrowsAsync<DbUpdateException>(() => repo.AddAsync(a2, ct: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies UpdateRole changes the role and raises an updated domain event.
    /// </summary>
    [Fact]
    public async Task UpdateRole_Changes_Role_And_Raises_Event()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new UserParentAssignmentRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("Role Change Parent");
        await parentRepo.AddAsync(parent, TestContext.Current.CancellationToken);

        var assignment = UserParentAssignment.Create("user-role", parent.CtrlNbr.Value, Roles.Employee);
        await repo.AddAsync(assignment, TestContext.Current.CancellationToken);

        // Act
        assignment.UpdateRole(Roles.ParentAdmin);
        await repo.UpdateAsync(assignment, TestContext.Current.CancellationToken);

        // Re-fetch
        var updated = await repo.GetByCtrlNbrAsync(assignment.CtrlNbr, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(Roles.ParentAdmin, updated.Role);
        // Created event + Updated event
        Assert.Equal(2, assignment.DomainEvents.Count);
        Assert.IsType<CrewService.Domain.DomainEvents.UserAccess.UserParentAssignmentUpdatedDomainEvent>(assignment.DomainEvents[1]);
    }

    /// <summary>
    /// Verifies UpdateRole with the same value does NOT raise a domain event.
    /// </summary>
    [Fact]
    public void UpdateRole_Same_Value_Does_Not_Raise_Event()
    {
        var assignment = UserParentAssignment.Create("user-noop", 250101120000001, Roles.ParentAdmin);
        var initialCount = assignment.DomainEvents.Count;

        assignment.UpdateRole(Roles.ParentAdmin);

        Assert.Equal(initialCount, assignment.DomainEvents.Count);
    }

    /// <summary>
    /// Verifies that soft-deleting an assignment via Remove() excludes it from queries.
    /// </summary>
    [Fact]
    public async Task Remove_SoftDeletes_Assignment()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new UserParentAssignmentRepository(ctx, _factory.CurrentUserService);

        var parent = Parent.Create("Delete Parent");
        await parentRepo.AddAsync(parent, TestContext.Current.CancellationToken);

        var assignment = UserParentAssignment.Create("user-del", parent.CtrlNbr.Value, "Dispatcher");
        await repo.AddAsync(assignment, TestContext.Current.CancellationToken);

        // Act � soft-delete via repository
        await repo.DeleteAsync(assignment.CtrlNbr, TestContext.Current.CancellationToken);

        var remaining = await repo.GetByUserIdAsync("user-del");

        // Assert � soft-deleted row should be excluded by global query filter
        Assert.Empty(remaining);
    }

    /// <summary>
    /// Verifies that a user can be assigned to different parents with different roles.
    /// </summary>
    [Fact]
    public async Task User_Can_Have_Different_Roles_Per_Parent()
    {
        // Arrange
        using var ctx = _factory.CreateContext();
        var parentRepo = new ParentRepository(ctx, _factory.CurrentUserService);
        var repo = new UserParentAssignmentRepository(ctx, _factory.CurrentUserService);

        var parentA = Parent.Create("Corp A");
        await parentRepo.AddAsync(parentA, TestContext.Current.CancellationToken);

        var parentB = Parent.Create("Corp B");
        await parentRepo.AddAsync(parentB, TestContext.Current.CancellationToken);

        var a1 = UserParentAssignment.Create("user-roles", parentA.CtrlNbr.Value, Roles.ParentAdmin);
        await repo.AddAsync(a1, TestContext.Current.CancellationToken);

        var a2 = UserParentAssignment.Create("user-roles", parentB.CtrlNbr.Value, Roles.Employee);
        await repo.AddAsync(a2, TestContext.Current.CancellationToken);

        // Act
        var forUser = await repo.GetByUserIdAsync("user-roles");
        var adminAssignments = await repo.GetByUserAndParentAsync("user-roles", parentA.CtrlNbr.Value);
        var readOnlyAssignments = await repo.GetByUserAndParentAsync("user-roles", parentB.CtrlNbr.Value);

        // Assert
        Assert.Equal(2, forUser.Count);
        Assert.Single(adminAssignments);
        Assert.Equal(Roles.ParentAdmin, adminAssignments[0].Role);
        Assert.Single(readOnlyAssignments);
        Assert.Equal(Roles.Employee, readOnlyAssignments[0].Role);
    }

    /// <summary>
    /// Verifies Create throws ArgumentException when userId is null or empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_With_Invalid_UserId_Throws(string? userId)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            UserParentAssignment.Create(userId!, 250101120000001, "Dispatcher"));
    }

    /// <summary>
    /// Verifies Create throws ArgumentException when role is null or empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_With_Invalid_Role_Throws(string? role)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            UserParentAssignment.Create("user-001", 250101120000001, role!));
    }

    /// <summary>
    /// Verifies UpdateRole throws ArgumentException when role is null or empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UpdateRole_With_Invalid_Role_Throws(string? role)
    {
        var assignment = UserParentAssignment.Create("user-001", 250101120000001, Roles.Employee);

        Assert.ThrowsAny<ArgumentException>(() => assignment.UpdateRole(role!));
    }

    /// <summary>
    /// Verifies Delete raises UserParentAssignmentDeletedDomainEvent.
    /// </summary>
    [Fact]
    public void Delete_Raises_DeletedDomainEvent()
    {
        var assignment = UserParentAssignment.Create("user-001", 250101120000001, "Dispatcher");
        var initialCount = assignment.DomainEvents.Count;

        assignment.Delete();

        Assert.Equal(initialCount + 1, assignment.DomainEvents.Count);
        Assert.IsType<UserParentAssignmentDeletedDomainEvent>(assignment.DomainEvents[^1]);
    }

    /// <summary>
    /// Verifies that Roles.RequiresRailroad returns expected results for known roles.
    /// SystemAdmin and ParentAdmin are parent-scoped; all others require a railroad.
    /// </summary>
    [Fact]
    public void Roles_RequiresRailroad_Returns_Expected_Results()
    {
        Assert.False(Roles.RequiresRailroad(Roles.SystemAdmin));
        Assert.False(Roles.RequiresRailroad(Roles.ParentAdmin));
        Assert.True(Roles.RequiresRailroad(Roles.RailroadAdmin));
        Assert.True(Roles.RequiresRailroad(Roles.Employee));
        Assert.True(Roles.RequiresRailroad("CraftManager"));
        Assert.True(Roles.RequiresRailroad("Dispatcher"));
        Assert.True(Roles.RequiresRailroad("CustomRole"));
    }
}
