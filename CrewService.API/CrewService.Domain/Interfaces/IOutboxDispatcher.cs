using CrewService.Domain.Outbox;

namespace CrewService.Domain.Interfaces;

/// <summary>
/// Dispatches outbox messages for immediate in-process publishing after commit.
/// </summary>
public interface IOutboxDispatcher
{
    void EnqueueForDispatch(IReadOnlyList<OutboxMessage> messages);
}