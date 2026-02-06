using CrewService.Domain.Interfaces;
using System.Text.Json;

namespace CrewService.Domain.DomainEvents;

public abstract record DomainEvent : IDomainEvent
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly JsonSerializerOptions DefaultOptions = new() { WriteIndented = false };

    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public string AggregateType { get; init; } = string.Empty;
    public long AggregateId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public string? OrchestrationId { get; init; }
    public string? IdempotencyKey { get; init; }
    public int EventVersion { get; init; } = 1;
    public string? PayloadJson { get; init; }

    protected DomainEvent(string aggregateType, long aggregateId, object? payload = null)
    {
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        EventType = GetType().Name;
        PayloadJson = payload is null
            ? null
            : JsonSerializer.Serialize(payload, CamelCaseOptions);
    }

    public override string ToString() => JsonSerializer.Serialize(this, DefaultOptions);
}
