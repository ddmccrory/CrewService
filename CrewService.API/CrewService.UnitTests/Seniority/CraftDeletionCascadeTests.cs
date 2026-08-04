using CrewService.Application.SeniorityOps;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Parents;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Persistance.Data;
using CrewService.Persistance.UnitOfWork;
using CrewService.UnitTests.Fixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrewService.UnitTests.Crafts;

public sealed class CraftDeletionCascadeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CrewServiceDbContext _crewContext;
    private readonly UserAccessDbContext _userContext;
    private readonly ICurrentUserService _currentUser = new TestCurrentUserService();

    public CraftDeletionCascadeTests()
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
    public async Task DeleteCraftAsync_SoftDeletes_AssociatedRosterBoards()
    {
        var (craftCtrlNbr, boardCtrlNbrs) = await SeedCraftWithBoardsAsync(TestContext.Current.CancellationToken);

        var sut = BuildService();
        await sut.DeleteCraftAsync(craftCtrlNbr, TestContext.Current.CancellationToken);

        await using var verifyContext = CreateReadContext();

        var deletedCraft = await verifyContext.Set<Craft>()
            .IgnoreQueryFilters()
            .SingleAsync(c => c.CtrlNbr == craftCtrlNbr, TestContext.Current.CancellationToken);
        Assert.True(deletedCraft.IsDeleted);

        var deletedBoards = await verifyContext.Set<RosterBoard>()
            .IgnoreQueryFilters()
            .Where(b => boardCtrlNbrs.Contains(b.CtrlNbr))
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(boardCtrlNbrs.Count, deletedBoards.Count);
        Assert.All(deletedBoards, b => Assert.True(b.IsDeleted));

        var deletedRoles = await verifyContext.Set<CraftRole>()
            .IgnoreQueryFilters()
            .Where(r => r.CraftCtrlNbr == craftCtrlNbr)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(deletedRoles);
        Assert.All(deletedRoles, r => Assert.True(r.IsDeleted));
    }

    private CraftAppService BuildService()
    {
        var factory = new OrchestrationUnitOfWorkFactory(
            _connection,
            _currentUser,
            new TestFieldEncryptor(),
            NullLoggerFactory.Instance);

        return new CraftAppService(factory, new CrewService.Application.TenantConfig.RailroadResolver());
    }

    private async Task<(CrewService.Domain.ValueObjects.ControlNumber CraftCtrlNbr, List<CrewService.Domain.ValueObjects.ControlNumber> BoardCtrlNbrs)> SeedCraftWithBoardsAsync(CancellationToken ct)
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

        var workAreaType = GroupType.Create("Assignment", null, true, parentCtrlNbr: parent.CtrlNbr);
        context.Set<GroupType>().Add(workAreaType);
        await context.SaveChangesAsync(ct);

        var workArea = DynamicGroup.Create(
            workAreaType.CtrlNbr,
            "Terminal A",
            railroad.CtrlNbr,
            null,
            true,
            "WA",
            parentCtrlNbr: parent.CtrlNbr,
            railroadCtrlNbr: railroad.CtrlNbr);
        context.Set<DynamicGroup>().Add(workArea);
        await context.SaveChangesAsync(ct);

        var craft = Craft.Create(
            parent.CtrlNbr,
            railroad.CtrlNbr,
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
            0);
        context.Set<Craft>().Add(craft);
        await context.SaveChangesAsync(ct);

        var roster = Roster.Create(craft.CtrlNbr, workArea.CtrlNbr, null, "Yardmaster", "Yardmasters", 1);
        context.Set<Roster>().Add(roster);
        await context.SaveChangesAsync(ct);

        var extra = RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr, "Yardmaster Extra Board", BoardType.ExtraBoard);
        var hangout = RosterBoard.Create(craft.CtrlNbr, roster.CtrlNbr, "Yardmaster Hangout", BoardType.Hangout);
        context.Set<RosterBoard>().AddRange(extra, hangout);
        var role = CraftRole.Create(craft.CtrlNbr, "YD", "Yardmaster", "Yardmaster");
        context.Set<CraftRole>().Add(role);
        await context.SaveChangesAsync(ct);

        return (craft.CtrlNbr, [extra.CtrlNbr, hangout.CtrlNbr]);
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