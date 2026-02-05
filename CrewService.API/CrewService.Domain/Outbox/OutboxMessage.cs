namespace CrewService.Domain.Outbox;

/// <summary>
/// Represents a domain event stored for reliable, transactional publication.
/// </summary>
public sealed class OutboxMessage
{
    public Guid MessageId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string AggregateType { get; private set; } = string.Empty;
    public long AggregateId { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }
    public string? OrchestrationId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public int EventVersion { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public OutboxMessageStatus Status { get; private set; }
    public int Retries { get; private set; }

    private OutboxMessage() { }

    private OutboxMessage(
        Guid messageId,
        string eventType,
        string aggregateType,
        long aggregateId,
        string payloadJson,
        string? correlationId,
        string? orchestrationId,
        string? idempotencyKey,
        int eventVersion)
    {
        MessageId = messageId;
        EventType = eventType;
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        PayloadJson = payloadJson;
        CorrelationId = correlationId;
        OrchestrationId = orchestrationId;
        IdempotencyKey = idempotencyKey;
        EventVersion = eventVersion;
        CreatedAt = DateTime.UtcNow;
        Status = OutboxMessageStatus.Pending;
        Retries = 0;
    }

    public static OutboxMessage Create(
        Guid messageId,
        string eventType,
        string aggregateType,
        long aggregateId,
        string payloadJson,
        string? correlationId = null,
        string? orchestrationId = null,
        string? idempotencyKey = null,
        int eventVersion = 1)
    {
        return new OutboxMessage(
            messageId,
            eventType,
            aggregateType,
            aggregateId,
            payloadJson,
            correlationId,
            orchestrationId,
            idempotencyKey,
            eventVersion);
    }

    public void MarkAsPublished()
    {
        Status = OutboxMessageStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = OutboxMessageStatus.Failed;
        Retries++;
    }

    public void ResetForRetry()
    {
        Status = OutboxMessageStatus.Pending;
    }
}