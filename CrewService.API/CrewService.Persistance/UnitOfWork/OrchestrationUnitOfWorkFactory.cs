using System.Data.Common;
using CrewService.Domain.Interfaces;
using CrewService.Persistance.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace CrewService.Persistance.UnitOfWork;

internal sealed class OrchestrationUnitOfWorkFactory(
    SqliteConnection connection,
    CrewServiceDbContext crewContext,
    UserAccessDbContext userContext,
    ICurrentUserService currentUserService,
    ILoggerFactory loggerFactory,
    IOutboxDispatcher? dispatcher = null,
    IDomainEventReactor? reactor = null) : IOrchestrationUnitOfWorkFactory
{
    public async Task<IOrchestrationUnitOfWork> CreateAsync(
        OrchestrationUnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new OrchestrationUnitOfWorkOptions();

        var correlationId = options.CorrelationId ?? Guid.NewGuid().ToString();
        var orchestrationId = Guid.NewGuid().ToString();
        var logger = loggerFactory.CreateLogger<OrchestrationUnitOfWork>();

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var existingTransaction = crewContext.Database.CurrentTransaction?.GetDbTransaction()
            ?? userContext.Database.CurrentTransaction?.GetDbTransaction();

        var ownsTransaction = existingTransaction is null;
        var transaction = existingTransaction ?? await connection.BeginTransactionAsync(cancellationToken);

        if (ownsTransaction)
        {
            await crewContext.Database.UseTransactionAsync((DbTransaction)transaction, cancellationToken);
            await userContext.Database.UseTransactionAsync((DbTransaction)transaction, cancellationToken);
        }

        return new OrchestrationUnitOfWork(
            (DbTransaction)transaction,
            crewContext,
            userContext,
            currentUserService,
            correlationId,
            orchestrationId,
            options.IdempotencyKey,
            ownsTransaction,
            logger,
            dispatcher,
            options.SuppressReactor ? null : reactor);
    }
}