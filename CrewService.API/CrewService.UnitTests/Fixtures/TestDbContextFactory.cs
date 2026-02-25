using CrewService.Domain.Interfaces;
using CrewService.Persistance.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CrewService.UnitTests.Fixtures;

internal sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ICurrentUserService _currentUserService = new TestCurrentUserService();

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public CrewServiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new CrewServiceDbContext(options, _currentUserService);
    }

    public ICurrentUserService CurrentUserService => _currentUserService;

    public void Dispose()
    {
        _connection.Dispose();
    }
}
