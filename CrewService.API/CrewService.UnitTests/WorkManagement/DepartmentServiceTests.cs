using CrewService.Application.WorkManagement;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Models.Employment;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.UnitOfWork;
using CrewService.UnitTests.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CrewService.UnitTests.WorkManagement;

public sealed class DepartmentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CrewServiceDbContext _crewContext;
    private readonly UserAccessDbContext _userContext;
    private readonly ICurrentUserService _currentUser = new TestCurrentUserService();

    public DepartmentServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var crewOptions = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(_connection)
            .Options;
        _crewContext = new CrewServiceDbContext(crewOptions, _currentUser, new TestFieldEncryptor());
        _crewContext.Database.EnsureCreated();

        var userOptions = new DbContextOptionsBuilder<UserAccessDbContext>()
            .UseSqlite(_connection)
            .Options;
        _userContext = new UserAccessDbContext(userOptions);
    }

    [Fact]
    public async Task CreateAsync_CreatesDefaultDepartmentReassignmentRule()
    {
        var (parentCtrlNbr, railroadCtrlNbr) = await SeedParentAndRailroadAsync(TestContext.Current.CancellationToken);

        var service = BuildService();
        var department = await service.CreateAsync(parentCtrlNbr, railroadCtrlNbr, "Transportation", "Vertical");

        await using var verifyContext = CreateReadContext();
        var rule = await verifyContext.Set<DepartmentReassignmentRule>()
            .SingleOrDefaultAsync(r => r.DepartmentCtrlNbr == department.CtrlNbr, TestContext.Current.CancellationToken);

        Assert.NotNull(rule);
        Assert.Equal(BoardType.Hangout, rule!.TargetBoardType);
        Assert.True(rule.IsRequired);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_DepartmentCraftsAndCraftRoles()
    {
        var (parentCtrlNbr, railroadCtrlNbr) = await SeedParentAndRailroadAsync(TestContext.Current.CancellationToken);

        await using (var seed = CreateReadContext())
        {
            var department = Department.Create(parentCtrlNbr, railroadCtrlNbr, "Transportation", "Vertical");
            seed.Set<Department>().Add(department);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);

            var craft = Craft.Create(
                parentCtrlNbr,
                railroadCtrlNbr,
                "Yardmaster",
                "Yardmasters",
                1,
                false,
                false,
                0,
                0,
                0,
                0,
                0,
                false,
                false,
                false,
                0,
                department.CtrlNbr);

            seed.Set<Craft>().Add(craft);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);

            var role = CraftRole.Create(craft.CtrlNbr, "YD", "Yardmaster", "Yardmaster");
            seed.Set<CraftRole>().Add(role);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var service = BuildService();

        await using (var verifySeed = CreateReadContext())
        {
            var departmentCtrlNbr = await verifySeed.Set<Department>()
                .Where(d => d.Name == "Transportation")
                .Select(d => d.CtrlNbr)
                .SingleAsync(TestContext.Current.CancellationToken);

            await service.DeleteAsync(departmentCtrlNbr);
        }

        await using var verify = CreateReadContext();

        var deletedDepartment = await verify.Set<Department>()
            .IgnoreQueryFilters()
            .SingleAsync(d => d.Name == "Transportation", TestContext.Current.CancellationToken);
        Assert.True(deletedDepartment.IsDeleted);

        var deletedCrafts = await verify.Set<Craft>()
            .IgnoreQueryFilters()
            .Where(c => c.DepartmentCtrlNbr == deletedDepartment.CtrlNbr)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.NotEmpty(deletedCrafts);
        Assert.All(deletedCrafts, c => Assert.True(c.IsDeleted));

        var craftIds = deletedCrafts.Select(c => c.CtrlNbr).ToHashSet();
        var deletedRoles = await verify.Set<CraftRole>()
            .IgnoreQueryFilters()
            .Where(r => craftIds.Contains(r.CraftCtrlNbr))
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.NotEmpty(deletedRoles);
        Assert.All(deletedRoles, r => Assert.True(r.IsDeleted));
    }

    [Fact]
    public async Task GetByParentAndRailroadAsync_WithCallSheetPermission_ReturnsAllContextDepartments()
    {
        var ct = TestContext.Current.CancellationToken;
        var (parentCtrlNbr, railroadCtrlNbr) = await SeedParentAndRailroadAsync(ct);

        await using (var seed = CreateReadContext())
        {
            seed.Set<Department>().AddRange(
                Department.Create(parentCtrlNbr, railroadCtrlNbr, "Transportation", "Vertical"),
                Department.Create(parentCtrlNbr, railroadCtrlNbr, "Mechanical", "Vertical"));

            var role = Role.Create("RailroadAdmin", "Railroad admin", isSystem: true, level: 80);
            var feature = Feature.Create("daily-operations/call-sheet", "Call Sheet", "Daily Operations", "/daily-operations/call-sheet");
            var permission = Permission.Create(role.CtrlNbr, feature.CtrlNbr, AccessLevel.FullAccess, parentCtrlNbr, null);
            var assignment = UserParentAssignment.Create(_currentUser.GetUserIdentifier()!, parentCtrlNbr, role.Name, railroadCtrlNbr);

            seed.Set<Role>().Add(role);
            seed.Set<Feature>().Add(feature);
            seed.Set<Permission>().Add(permission);
            seed.Set<UserParentAssignment>().Add(assignment);

            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService();
        var departments = await service.GetByParentAndRailroadAsync(parentCtrlNbr, railroadCtrlNbr);

        Assert.Equal(2, departments.Count);
        Assert.Contains(departments, d => d.Name == "Transportation");
        Assert.Contains(departments, d => d.Name == "Mechanical");
    }

    [Fact]
    public async Task GetByParentAndRailroadAsync_SystemAdminRoleWithoutAssignmentsOrEmployee_ReturnsAllContextDepartments()
    {
        var ct = TestContext.Current.CancellationToken;
        var (parentCtrlNbr, railroadCtrlNbr) = await SeedParentAndRailroadAsync(ct);

        await using (var seed = CreateReadContext())
        {
            seed.Set<Department>().AddRange(
                Department.Create(parentCtrlNbr, railroadCtrlNbr, "Transportation", "Vertical"),
                Department.Create(parentCtrlNbr, railroadCtrlNbr, "Mechanical", "Vertical"));
            await seed.SaveChangesAsync(ct);
        }

        var systemAdminUser = new RoleCurrentUserService(_currentUser.GetUserId(), _currentUser.GetUserName(), Roles.SystemAdmin);
        var service = BuildService(systemAdminUser);
        var departments = await service.GetByParentAndRailroadAsync(parentCtrlNbr, railroadCtrlNbr);

        Assert.Equal(2, departments.Count);
        Assert.Contains(departments, d => d.Name == "Transportation");
        Assert.Contains(departments, d => d.Name == "Mechanical");
    }

    [Fact]
    public async Task GetByParentAndRailroadAsync_SystemAdminWithoutEmployee_ReturnsAllContextDepartments()
    {
        var ct = TestContext.Current.CancellationToken;
        var (parentCtrlNbr, railroadCtrlNbr) = await SeedParentAndRailroadAsync(ct);

        await using (var seed = CreateReadContext())
        {
            seed.Set<Department>().AddRange(
                Department.Create(parentCtrlNbr, railroadCtrlNbr, "Transportation", "Vertical"),
                Department.Create(parentCtrlNbr, railroadCtrlNbr, "Mechanical", "Vertical"));

            var role = Role.Create(Roles.SystemAdmin, "System admin", isSystem: true, level: 100);
            var assignment = UserParentAssignment.Create(_currentUser.GetUserIdentifier()!, parentCtrlNbr, role.Name);

            seed.Set<Role>().Add(role);
            seed.Set<UserParentAssignment>().Add(assignment);

            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService();
        var departments = await service.GetByParentAndRailroadAsync(parentCtrlNbr, railroadCtrlNbr);

        Assert.Equal(2, departments.Count);
        Assert.Contains(departments, d => d.Name == "Transportation");
        Assert.Contains(departments, d => d.Name == "Mechanical");
    }

    [Fact]
    public async Task GetByParentAndRailroadAsync_WithoutPermissionAndWithoutEmployee_ReturnsEmpty()
    {
        var ct = TestContext.Current.CancellationToken;
        var (parentCtrlNbr, railroadCtrlNbr) = await SeedParentAndRailroadAsync(ct);

        await using (var seed = CreateReadContext())
        {
            seed.Set<Department>().Add(Department.Create(parentCtrlNbr, railroadCtrlNbr, "Transportation", "Vertical"));

            var role = Role.Create("Dispatcher", "Dispatcher", isSystem: false, level: 50);
            var assignment = UserParentAssignment.Create(_currentUser.GetUserIdentifier()!, parentCtrlNbr, role.Name, railroadCtrlNbr);

            seed.Set<Role>().Add(role);
            seed.Set<UserParentAssignment>().Add(assignment);
            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService();
        var departments = await service.GetByParentAndRailroadAsync(parentCtrlNbr, railroadCtrlNbr);

        Assert.Empty(departments);
    }

    [Fact]
    public async Task GetByParentAndRailroadAsync_EmployeeWithoutRosterAndWithoutPermission_ReturnsEmpty()
    {
        var ct = TestContext.Current.CancellationToken;
        var (parentCtrlNbr, railroadCtrlNbr) = await SeedParentAndRailroadAsync(ct);

        await using (var seed = CreateReadContext())
        {
            var department = Department.Create(parentCtrlNbr, railroadCtrlNbr, "Transportation", "Vertical");
            seed.Set<Department>().Add(department);

            var role = Role.Create("Employee", "Employee", isSystem: true, level: 10);
            var assignment = UserParentAssignment.Create(_currentUser.GetUserIdentifier()!, parentCtrlNbr, role.Name, railroadCtrlNbr);
            seed.Set<Role>().Add(role);
            seed.Set<UserParentAssignment>().Add(assignment);

            var employmentStatus = EmploymentStatus.Create(railroadCtrlNbr, "ACT", "Active", 1, "A");
            seed.EmploymentStatuses.Add(employmentStatus);
            await seed.SaveChangesAsync(ct);

            var employee = Employee.Create(
                clientCtrlNbr: railroadCtrlNbr,
                userId: _currentUser.GetUserIdentifier()!,
                employeeNumber: "EMP001",
                ssn: "123-45-6789",
                gender: Gender.Male,
                race: Race.White,
                birthDate: new DateTime(1990, 1, 1),
                employmentDate: new DateTime(2020, 1, 1),
                employmentStatusCtrlNbr: employmentStatus.CtrlNbr,
                email: "emp001@example.com",
                invitedByUserId: "system",
                invitedByUserName: "System");

            seed.Employees.Add(employee);
            await seed.SaveChangesAsync(ct);
        }

        var service = BuildService();
        var departments = await service.GetByParentAndRailroadAsync(parentCtrlNbr, railroadCtrlNbr);

        Assert.Empty(departments);
    }

    private DepartmentService BuildService(ICurrentUserService? currentUser = null)
    {
        var effectiveCurrentUser = currentUser ?? _currentUser;
        var factory = new OrchestrationUnitOfWorkFactory(
            _connection,
            effectiveCurrentUser,
            new TestFieldEncryptor(),
            NullLoggerFactory.Instance);

        return new DepartmentService(factory, effectiveCurrentUser);
    }

    private sealed class RoleCurrentUserService(Guid userId, string userName, params string[] roles) : ICurrentUserService
    {
        private readonly HashSet<string> _roles = new(roles, StringComparer.OrdinalIgnoreCase);

        public Guid GetUserId() => userId;
        public string GetUserName() => userName;
        public string? GetUserIdentifier() => userId.ToString();
        public bool IsInRole(string roleName) => _roles.Contains(roleName);
        public long? GetParentCtrlNbr() => null;
        public void SetAuditOverride(string name) { }
    }

    private async Task<(ControlNumber ParentCtrlNbr, ControlNumber RailroadCtrlNbr)> SeedParentAndRailroadAsync(CancellationToken ct)
    {
        await using var context = CreateReadContext();

        var parent = Parent.Create("Test Parent");
        context.Parents.Add(parent);
        await context.SaveChangesAsync(ct);

        var railroadType = GroupType.Create("Railroad", null, true);
        context.Set<GroupType>().Add(railroadType);
        await context.SaveChangesAsync(ct);

        var railroad = DynamicGroup.Create(
            railroadType.CtrlNbr,
            "Test Railroad",
            null,
            null,
            false,
            "RR",
            parentCtrlNbr: parent.CtrlNbr);

        context.Set<DynamicGroup>().Add(railroad);
        await context.SaveChangesAsync(ct);

        return (parent.CtrlNbr, railroad.CtrlNbr);
    }

    private CrewServiceDbContext CreateReadContext()
    {
        var options = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new CrewServiceDbContext(options, _currentUser, new TestFieldEncryptor());
    }

    public void Dispose()
    {
        _crewContext.Dispose();
        _userContext.Dispose();
        _connection.Dispose();
    }
}
