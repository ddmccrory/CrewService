using CrewService.Application.WorkManagement;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Modules.WorkManagement;
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

    private DepartmentService BuildService()
    {
        var factory = new OrchestrationUnitOfWorkFactory(
            _connection,
            _crewContext,
            _userContext,
            _currentUser,
            NullLoggerFactory.Instance);

        return new DepartmentService(factory);
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
