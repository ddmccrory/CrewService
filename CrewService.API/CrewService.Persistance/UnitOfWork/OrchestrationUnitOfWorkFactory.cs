using System.Data.Common;
using CrewService.Domain.Interfaces;
using CrewService.Persistance.Encryption;
using CrewService.Persistance.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace CrewService.Persistance.UnitOfWork;

internal sealed class OrchestrationUnitOfWorkFactory : IOrchestrationUnitOfWorkFactory
{
    private readonly Func<SqliteConnection> _connectionFactory;
    private readonly bool _ownsConnection;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFieldEncryptor _fieldEncryptor;
    private readonly IWorkflowEffectExecutionGuard _workflowEffectExecutionGuard;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOutboxDispatcher? _dispatcher;
    private readonly IDomainEventReactor? _reactor;

    public OrchestrationUnitOfWorkFactory(
        Func<SqliteConnection> connectionFactory,
        ICurrentUserService currentUserService,
        IFieldEncryptor fieldEncryptor,
        IWorkflowEffectExecutionGuard workflowEffectExecutionGuard,
        ILoggerFactory loggerFactory,
        IOutboxDispatcher? dispatcher = null,
        IDomainEventReactor? reactor = null,
        bool ownsConnection = true)
    {
        _connectionFactory = connectionFactory;
        _ownsConnection = ownsConnection;
        _currentUserService = currentUserService;
        _fieldEncryptor = fieldEncryptor;
        _workflowEffectExecutionGuard = workflowEffectExecutionGuard;
        _loggerFactory = loggerFactory;
        _dispatcher = dispatcher;
        _reactor = reactor;
    }

    public OrchestrationUnitOfWorkFactory(
        Func<SqliteConnection> connectionFactory,
        ICurrentUserService currentUserService,
        IFieldEncryptor fieldEncryptor,
        ILoggerFactory loggerFactory,
        IOutboxDispatcher? dispatcher = null,
        IDomainEventReactor? reactor = null)
        : this(
            connectionFactory,
            currentUserService,
            fieldEncryptor,
            new NoOpWorkflowEffectExecutionGuard(),
            loggerFactory,
            dispatcher,
            reactor)
    {
    }

    public OrchestrationUnitOfWorkFactory(
        SqliteConnection sharedConnection,
        ICurrentUserService currentUserService,
        IFieldEncryptor fieldEncryptor,
        IWorkflowEffectExecutionGuard workflowEffectExecutionGuard,
        ILoggerFactory loggerFactory,
        IOutboxDispatcher? dispatcher = null,
        IDomainEventReactor? reactor = null)
        : this(
            () => sharedConnection,
            currentUserService,
            fieldEncryptor,
            workflowEffectExecutionGuard,
            loggerFactory,
            dispatcher,
            reactor,
            ownsConnection: false)
    {
    }

    public OrchestrationUnitOfWorkFactory(
        SqliteConnection sharedConnection,
        ICurrentUserService currentUserService,
        IFieldEncryptor fieldEncryptor,
        ILoggerFactory loggerFactory,
        IOutboxDispatcher? dispatcher = null,
        IDomainEventReactor? reactor = null)
        : this(
            sharedConnection,
            currentUserService,
            fieldEncryptor,
            new NoOpWorkflowEffectExecutionGuard(),
            loggerFactory,
            dispatcher,
            reactor)
    {
    }

    public async Task<IOrchestrationUnitOfWork> CreateAsync(
        OrchestrationUnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new OrchestrationUnitOfWorkOptions();

        if (_workflowEffectExecutionGuard.IsInWorkflowDbEffectExecution)
            throw new InvalidOperationException("Nested orchestration UoW creation is not allowed during workflow DB effect execution. Use the active IOrchestrationUnitOfWork instance.");

        var correlationId = options.CorrelationId ?? Guid.NewGuid().ToString();
        var orchestrationId = Guid.NewGuid().ToString();
        var logger = _loggerFactory.CreateLogger<OrchestrationUnitOfWork>();

        var connection = _connectionFactory();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var crewOptions = new DbContextOptionsBuilder<CrewServiceDbContext>()
            .UseSqlite(connection)
            .Options;
        var userOptions = new DbContextOptionsBuilder<UserAccessDbContext>()
            .UseSqlite(connection)
            .Options;

        var crewContext = new CrewServiceDbContext(crewOptions, _currentUserService, _fieldEncryptor);
        var userContext = new UserAccessDbContext(userOptions);

        var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await crewContext.Database.UseTransactionAsync((DbTransaction)transaction, cancellationToken);
        await userContext.Database.UseTransactionAsync((DbTransaction)transaction, cancellationToken);

        var ownsTransaction = true;

        return new OrchestrationUnitOfWork(
            (DbTransaction)transaction,
            connection,
            _ownsConnection,
            crewContext,
            userContext,
            _currentUserService,
            correlationId,
            orchestrationId,
            options.IdempotencyKey,
            ownsTransaction,
            logger,
            _dispatcher,
            options.SuppressReactor ? null : _reactor);
    }

    private sealed class NoOpWorkflowEffectExecutionGuard : IWorkflowEffectExecutionGuard
    {
        public bool IsInWorkflowDbEffectExecution => false;

        public IDisposable BeginWorkflowDbEffectExecutionScope() => NoOpScope.Instance;

        private sealed class NoOpScope : IDisposable
        {
            internal static readonly NoOpScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}