using System.Data.Common;
using CrewService.Domain.DomainEvents;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Outbox;
using CrewService.Domain.Primitives;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CrewService.Persistance.UnitOfWork;

/// <summary>
/// Short-lived orchestration UoW that creates a single shared DbConnection + DbTransaction
/// and instantiates one or both DbContexts using the same connection.
/// Provides access to all repositories for various orchestration scenarios.
/// </summary>
internal sealed class OrchestrationUnitOfWork : IOrchestrationUnitOfWork
{
    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;
    private readonly CrewServiceDbContext _crewContext;
    private readonly ILogger<OrchestrationUnitOfWork> _logger;
    private readonly string _idempotencyKey;
    private readonly IOutboxDispatcher? _dispatcher;

    private bool _committed;
    private bool _disposed;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Core Employee / Railroad
    // ──────────────────────────────────────────────────────────────────
    private IEmployeeRepository? _employees;
    private IRailroadEmployeeRepository? _railroadEmployees;
    private IRailroadPoolEmployeeRepository? _railroadPoolEmployees;
    private IRailroadRepository? _railroads;
    private IRailroadPoolRepository? _railroadPools;
    private IParentRepository? _parents;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: ContactTypes
    // ──────────────────────────────────────────────────────────────────
    private IAddressTypeRepository? _addressTypes;
    private IPhoneNumberTypeRepository? _phoneNumberTypes;
    private IEmailAddressTypeRepository? _emailAddressTypes;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Employment
    // ──────────────────────────────────────────────────────────────────
    private IEmploymentStatusRepository? _employmentStatuses;
    private IEmploymentStatusHistoryRepository? _employmentStatusHistory;
    private IEmployeePriorServiceCreditRepository? _employeePriorServiceCredits;

    // ──────────────────────────────────────────────────────────────────
    // Lazy-initialized repositories: Seniority
    // ──────────────────────────────────────────────────────────────────
    private ICraftRepository? _crafts;
    private IRosterRepository? _rosters;
    private ISeniorityRepository? _seniority;
    private ISeniorityStateRepository? _seniorityStates;

    public string CorrelationId { get; }
    public string OrchestrationId { get; }

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Core Employee / Railroad
    // ──────────────────────────────────────────────────────────────────
    public IEmployeeRepository Employees => _employees ??= new EmployeeRepository(_crewContext);
    public IRailroadEmployeeRepository RailroadEmployees => _railroadEmployees ??= new RailroadEmployeeRepository(_crewContext);
    public IRailroadPoolEmployeeRepository RailroadPoolEmployees => _railroadPoolEmployees ??= new RailroadPoolEmployeeRepository(_crewContext);
    public IRailroadRepository Railroads => _railroads ??= new RailroadRepository(_crewContext);
    public IRailroadPoolRepository RailroadPools => _railroadPools ??= new RailroadPoolRepository(_crewContext);
    public IParentRepository Parents => _parents ??= new ParentRepository(_crewContext);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: ContactTypes
    // ──────────────────────────────────────────────────────────────────
    public IAddressTypeRepository AddressTypes => _addressTypes ??= new AddressTypeRepository(_crewContext);
    public IPhoneNumberTypeRepository PhoneNumberTypes => _phoneNumberTypes ??= new PhoneNumberTypeRepository(_crewContext);
    public IEmailAddressTypeRepository EmailAddressTypes => _emailAddressTypes ??= new EmailAddressTypeRepository(_crewContext);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Employment
    // ──────────────────────────────────────────────────────────────────
    public IEmploymentStatusRepository EmploymentStatuses => _employmentStatuses ??= new EmploymentStatusRepository(_crewContext);
    public IEmploymentStatusHistoryRepository EmploymentStatusHistory => _employmentStatusHistory ??= new EmploymentStatusHistoryRepository(_crewContext);
    public IEmployeePriorServiceCreditRepository EmployeePriorServiceCredits => _employeePriorServiceCredits ??= new EmployeePriorServiceCreditRepository(_crewContext);

    // ──────────────────────────────────────────────────────────────────
    // Repository Properties: Seniority
    // ──────────────────────────────────────────────────────────────────
    public ICraftRepository Crafts => _crafts ??= new CraftRepository(_crewContext);
    public IRosterRepository Rosters => _rosters ??= new RosterRepository(_crewContext);
    public ISeniorityRepository Seniority => _seniority ??= new SeniorityRepository(_crewContext);
    public ISeniorityStateRepository SeniorityStates => _seniorityStates ??= new SeniorityStateRepository(_crewContext);

    internal OrchestrationUnitOfWork(
        DbConnection connection,
        DbTransaction transaction,
        CrewServiceDbContext crewContext,
        string correlationId,
        string orchestrationId,
        string? idempotencyKey,
        ILogger<OrchestrationUnitOfWork> logger,
        IOutboxDispatcher? dispatcher = null)  // Add this parameter
    {
        _connection = connection;
        _transaction = transaction;
        _crewContext = crewContext;
        CorrelationId = correlationId;
        OrchestrationId = orchestrationId;
        _idempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString();
        _logger = logger;
        _dispatcher = dispatcher;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_committed)
            throw new InvalidOperationException("Transaction has already been committed.");

        ObjectDisposedException.ThrowIf(_disposed, typeof(OrchestrationUnitOfWork));

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Committing orchestration UoW. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}",
                    CorrelationId, OrchestrationId);
            }

            // Collect domain events from all tracked entities
            var domainEvents = CollectDomainEvents();

            // Convert domain events to OutboxMessage rows
            var outboxMessages = new List<OutboxMessage>();
            foreach (var domainEvent in domainEvents)
            {
                var outboxMessage = CreateOutboxMessage(domainEvent);
                outboxMessages.Add(outboxMessage);
                _crewContext.OutboxMessages.Add(outboxMessage);
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var eventCount = domainEvents.Count;
                _logger.LogDebug(
                    "Persisting {EventCount} domain events to outbox. OrchestrationId: {OrchestrationId}",
                    eventCount, OrchestrationId);
            }

            // Save all entity changes + outbox rows in single SaveChanges
            await _crewContext.SaveChangesAsync(cancellationToken);

            // Commit the shared transaction
            await _transaction.CommitAsync(cancellationToken);

            _committed = true;

            // Dispatch messages for immediate publishing (if dispatcher available)
            if (_dispatcher is not null && outboxMessages.Count > 0)
            {
                _dispatcher.EnqueueForDispatch(outboxMessages);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Dispatched {Count} messages for immediate publishing.", outboxMessages.Count);
                }
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var outboxCount = outboxMessages.Count;
                _logger.LogInformation(
                    "Orchestration UoW committed successfully. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}, EventsWritten: {EventCount}",
                    CorrelationId, OrchestrationId, outboxCount);
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex,
                    "Orchestration UoW commit failed. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}",
                    CorrelationId, OrchestrationId);
            }

            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_committed || _disposed)
            return;

        try
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Rolling back orchestration UoW. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}",
                    CorrelationId, OrchestrationId);
            }

            await _transaction.RollbackAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex,
                    "Orchestration UoW rollback failed. CorrelationId: {CorrelationId}, OrchestrationId: {OrchestrationId}",
                    CorrelationId, OrchestrationId);
            }
        }
    }

    private List<DomainEvent> CollectDomainEvents()
    {
        var domainEvents = new List<DomainEvent>();

        // Collect from all tracked Entity instances
        var trackedEntities = _crewContext.ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in trackedEntities)
        {
            var events = entity.DomainEvents;
            foreach (var domainEvent in events.OfType<DomainEvent>())
            {
                // Enrich event with correlation/orchestration IDs
                var enrichedEvent = domainEvent with
                {
                    CorrelationId = CorrelationId,
                    OrchestrationId = OrchestrationId,
                    IdempotencyKey = $"{_idempotencyKey}:{domainEvent.EventType}:{domainEvent.AggregateId}"
                };
                domainEvents.Add(enrichedEvent);
            }
        }

        return domainEvents;
    }

    private static OutboxMessage CreateOutboxMessage(DomainEvent domainEvent)
    {
        return OutboxMessage.Create(
            messageId: domainEvent.EventId,
            eventType: domainEvent.EventType,
            aggregateType: domainEvent.AggregateType,
            aggregateId: domainEvent.AggregateId,
            payloadJson: domainEvent.ToString(),
            correlationId: domainEvent.CorrelationId,
            orchestrationId: domainEvent.OrchestrationId,
            idempotencyKey: domainEvent.IdempotencyKey,
            eventVersion: domainEvent.EventVersion);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsyncCore().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!_committed)
        {
            await RollbackAsync();
        }

        await _crewContext.DisposeAsync();
        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}