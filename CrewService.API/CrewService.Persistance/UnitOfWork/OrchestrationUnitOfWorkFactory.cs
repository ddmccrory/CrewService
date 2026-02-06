using System.Data.Common;
using CrewService.Domain.Interfaces;
using CrewService.Persistance.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CrewService.Persistance.UnitOfWork;

/// <summary>
/// Factory for creating short-lived orchestration UoW instances.
/// Creates a dedicated DbConnection + DbTransaction for each orchestration.
/// </summary>
internal sealed class OrchestrationUnitOfWorkFactory(
    IConfiguration configuration,
    ICurrentUserService currentUserService,
    ILoggerFactory loggerFactory) : IOrchestrationUnitOfWorkFactory
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public async Task<IOrchestrationUnitOfWork> CreateAsync(
        OrchestrationUnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new OrchestrationUnitOfWorkOptions();

        var correlationId = options.CorrelationId ?? Guid.NewGuid().ToString();
        var orchestrationId = Guid.NewGuid().ToString();

        var logger = _loggerFactory.CreateLogger<OrchestrationUnitOfWork>();

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "Creating orchestration UoW. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}",
                correlationId, orchestrationId);
        }

        // Get connection string
        var connectionString = _configuration.GetConnectionString("SQLiteConnection")
            ?? throw new InvalidOperationException("SQLiteConnection connection string not configured.");

        // Create and open a dedicated connection
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Begin a transaction on this connection
        var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Build DbContextOptions using the existing connection
        var crewContextOptions = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(connection)
            .Options;

        // Create the CrewServiceDbContext with the shared connection
        var crewContext = new CrewServiceDbContext(crewContextOptions, _currentUserService);

        // Enlist the context in the shared transaction
        await crewContext.Database.UseTransactionAsync(transaction, cancellationToken);

        return new OrchestrationUnitOfWork(
            connection,
            (DbTransaction)transaction,
            crewContext,
            correlationId,
            orchestrationId,
            options.IdempotencyKey,
            logger);
    }
}