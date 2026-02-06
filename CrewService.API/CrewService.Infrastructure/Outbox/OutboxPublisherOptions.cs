namespace CrewService.Infrastructure.Outbox;

/// <summary>
/// Configuration options for the outbox publisher background service.
/// </summary>
public sealed class OutboxPublisherOptions
{
    public const string SectionName = "OutboxPublisher";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 5;
    public bool Enabled { get; set; } = true;
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);
    public bool EnableCleanup { get; set; } = true;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}