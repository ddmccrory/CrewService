using CrewService.Application.WorkManagement;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Models.Parents;
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

    private DepartmentService BuildService()
    {
        var factory = new OrchestrationUnitOfWorkFactory(
            _connection,
            _crewContext,
            _userContext,
            _currentUser,
            NullLoggerFactory.Instance);

        return new DepartmentService(factory, _currentUser);
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
