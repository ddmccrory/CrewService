using CrewService.Domain.Interfaces.Repositories;

namespace CrewService.Domain.Interfaces;

/// <summary>
/// Short-lived orchestration Unit of Work for atomic multi-context flows.
/// Creates a single shared DbConnection + DbTransaction and instantiates contexts as needed.
/// Provides access to all repositories for various orchestration scenarios.
/// </summary>
public interface IOrchestrationUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Correlation ID for logging and event tracing across the orchestration.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Orchestration ID for grouping related domain events.
    /// </summary>
    string OrchestrationId { get; }

    // ──────────────────────────────────────────────────────────────────
    // Core Employee / Railroad Orchestration
    // ──────────────────────────────────────────────────────────────────
    IEmployeeRepository Employees { get; }
    IRailroadEmployeeRepository RailroadEmployees { get; }
    IRailroadPoolEmployeeRepository RailroadPoolEmployees { get; }
    IRailroadRepository Railroads { get; }
    IRailroadPoolRepository RailroadPools { get; }
    IParentRepository Parents { get; }

    // ──────────────────────────────────────────────────────────────────
    // ContactTypes
    // ──────────────────────────────────────────────────────────────────
    IAddressTypeRepository AddressTypes { get; }
    IPhoneNumberTypeRepository PhoneNumberTypes { get; }
    IEmailAddressTypeRepository EmailAddressTypes { get; }

    // ──────────────────────────────────────────────────────────────────
    // Employment
    // ──────────────────────────────────────────────────────────────────
    IEmploymentStatusRepository EmploymentStatuses { get; }
    IEmploymentStatusHistoryRepository EmploymentStatusHistory { get; }
    IEmployeePriorServiceCreditRepository EmployeePriorServiceCredits { get; }

    // ──────────────────────────────────────────────────────────────────
    // Seniority
    // ──────────────────────────────────────────────────────────────────
    ICraftRepository Crafts { get; }
    IRosterRepository Rosters { get; }
    ISeniorityRepository Seniority { get; }
    ISeniorityStateRepository SeniorityStates { get; }

    /// <summary>
    /// Collects domain events from tracked entities, persists OutboxMessage rows,
    /// saves all changes, and commits the transaction atomically.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction. Call on error before disposing.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}