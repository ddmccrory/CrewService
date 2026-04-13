namespace CrewService.Domain.DomainEvents;

/// <summary>
/// Permanent audit record for every domain event raised by any entity.
/// Not an Entity (no soft delete, no ControlNumber PK) — analogous to OutboxMessage.
/// </summary>
public sealed class DomainEventLog
{
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string AggregateType { get; private set; } = string.Empty;
    public long AggregateId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? PayloadJson { get; private set; }
    public string PerformedBy { get; private set; } = string.Empty;
    public DateTime LoggedAtUtc { get; private set; }
    public long? ParentCtrlNbr { get; private set; }

    private DomainEventLog() { }

    public static DomainEventLog Create(
        Guid eventId, string eventType, string aggregateType,
        long aggregateId, DateTime occurredAt, string? payloadJson,
        string performedBy, long? parentCtrlNbr = null)
    {
        return new DomainEventLog
        {
            EventId = eventId,
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            OccurredAt = occurredAt,
            PayloadJson = payloadJson,
            PerformedBy = performedBy,
            LoggedAtUtc = DateTime.UtcNow,
            ParentCtrlNbr = parentCtrlNbr
        };
    }
}
