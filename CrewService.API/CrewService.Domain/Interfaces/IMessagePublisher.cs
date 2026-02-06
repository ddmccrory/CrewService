namespace CrewService.Domain.Interfaces;

/// <summary>
/// Abstraction for publishing domain events to an external message bus.
/// </summary>
public interface IMessagePublisher
{
    Task<bool> PublishAsync(
        string eventType,
        string aggregateType,
        long aggregateId,
        string payloadJson,
        string? correlationId,
        Guid messageId,
        CancellationToken cancellationToken = default);
}