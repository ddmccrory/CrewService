using CrewService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CrewService.Infrastructure.Outbox;

/// <summary>
/// No-op message publisher for development and testing.
/// Logs messages but does not publish to any external bus.
/// Replace with a real implementation (Azure Service Bus, RabbitMQ, etc.) for production.
/// </summary>
public sealed class NoOpMessagePublisher(ILogger<NoOpMessagePublisher> logger) : IMessagePublisher
{
    private readonly ILogger<NoOpMessagePublisher> _logger = logger;

    public Task<bool> PublishAsync(
        string eventType,
        string aggregateType,
        long aggregateId,
        string payloadJson,
        string? correlationId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[NoOp] Would publish message. MessageId: {MessageId}, EventType: {EventType}, AggregateType: {AggregateType}, AggregateId: {AggregateId}, CorrelationId: {CorrelationId}",
                messageId, eventType, aggregateType, aggregateId, correlationId);
        }

        // Return true to mark as "published" in outbox
        return Task.FromResult(true);
    }
}