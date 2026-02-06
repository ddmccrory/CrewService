namespace CrewService.Domain.Interfaces;

/// <summary>
/// Factory for creating short-lived orchestration UoW instances.
/// Registered as transient in DI.
/// </summary>
public interface IOrchestrationUnitOfWorkFactory
{
    /// <summary>
    /// Creates a new orchestration UoW with the specified options.
    /// </summary>
    /// <param name="options">Configuration for which contexts to enlist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IOrchestrationUnitOfWork> CreateAsync(
        OrchestrationUnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for configuring which DbContexts the orchestration UoW should enlist.
/// </summary>
public sealed class OrchestrationUnitOfWorkOptions
{
    /// <summary>
    /// Whether to enlist the UserAccessDbContext (Identity). Default: false.
    /// </summary>
    public bool NeedUserContext { get; init; } = false;

    /// <summary>
    /// Whether to enlist the CrewServiceDbContext (domain entities). Default: true.
    /// </summary>
    public bool NeedCrewContext { get; init; } = true;

    /// <summary>
    /// Optional correlation ID for logging. If null, a new GUID is generated.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Optional idempotency key for duplicate prevention.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}