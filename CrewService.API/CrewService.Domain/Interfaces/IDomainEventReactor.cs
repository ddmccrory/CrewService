using CrewService.Domain.DomainEvents;

namespace CrewService.Domain.Interfaces;

/// <summary>
/// Dispatches committed domain events to in-process reactive handlers.
/// Mirrors <see cref="IOutboxDispatcher"/> but for local side-effects
/// (e.g. re-evaluating qualifications after an on-duty record is created).
/// </summary>
public interface IDomainEventReactor
{
    Task ReactAsync(IReadOnlyList<DomainEvent> events, CancellationToken cancellationToken = default);
}
