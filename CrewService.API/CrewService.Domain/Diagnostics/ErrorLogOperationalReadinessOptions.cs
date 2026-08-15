namespace CrewService.Domain.Diagnostics;

public sealed class ErrorLogOperationalReadinessOptions
{
    public const string SectionName = "ErrorLogOperationalReadiness";

    public bool EnableRetentionCleanup { get; set; } = true;
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(6);
    public bool EnableAlerting { get; set; } = true;
    public TimeSpan AlertWindow { get; set; } = TimeSpan.FromMinutes(30);
    public int AlertOccurrenceThreshold { get; set; } = 20;
    public string AlertSeverityMinimum { get; set; } = "Error";
    public string AlertChannel { get; set; } = "SystemSupport";
}
