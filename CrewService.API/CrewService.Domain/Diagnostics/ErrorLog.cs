namespace CrewService.Domain.Diagnostics;

public sealed class ErrorLog
{
    public Guid ErrorId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime FirstOccurredAtUtc { get; private set; }
    public DateTime LastOccurredAtUtc { get; private set; }
    public DateTime LoggedAtUtc { get; private set; }
    public string ErrorKind { get; private set; } = string.Empty;
    public string SourceApp { get; private set; } = string.Empty;
    public string SourceLayer { get; private set; } = string.Empty;
    public string Severity { get; private set; } = string.Empty;
    public string FingerprintHash { get; private set; } = string.Empty;
    public int OccurrenceCount { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? SuppressionReason { get; private set; }
    public string ErrorCode { get; private set; } = string.Empty;
    public string ExceptionType { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string TraceId { get; private set; } = string.Empty;
    public string? Route { get; private set; }
    public string? Method { get; private set; }
    public string PerformedBy { get; private set; } = string.Empty;
    public long? ParentCtrlNbr { get; private set; }
    public long? RailroadCtrlNbr { get; private set; }
    public string? PayloadJson { get; private set; }

    private ErrorLog() { }

    public static ErrorLog Create(
        DateTime occurredAtUtc,
        string errorKind,
        string sourceApp,
        string sourceLayer,
        string severity,
        string fingerprintHash,
        string errorCode,
        string exceptionType,
        string message,
        string traceId,
        string? route,
        string? method,
        string performedBy,
        long? parentCtrlNbr,
        long? railroadCtrlNbr,
        string? payloadJson)
    {
        return new ErrorLog
        {
            ErrorId = Guid.NewGuid(),
            OccurredAtUtc = EnsureUtc(occurredAtUtc),
            FirstOccurredAtUtc = EnsureUtc(occurredAtUtc),
            LastOccurredAtUtc = EnsureUtc(occurredAtUtc),
            LoggedAtUtc = DateTime.UtcNow,
            ErrorKind = errorKind,
            SourceApp = sourceApp,
            SourceLayer = sourceLayer,
            Severity = severity,
            FingerprintHash = fingerprintHash,
            OccurrenceCount = 1,
            Status = ErrorLogStatuses.New,
            ErrorCode = errorCode,
            ExceptionType = exceptionType,
            Message = message,
            TraceId = traceId,
            Route = route,
            Method = method,
            PerformedBy = performedBy,
            ParentCtrlNbr = parentCtrlNbr,
            RailroadCtrlNbr = railroadCtrlNbr,
            PayloadJson = payloadJson
        };
    }

    public void RegisterOccurrence(DateTime occurredAtUtc, string severity, string traceId, string? message, string? payloadJson)
    {
        var occurredUtc = EnsureUtc(occurredAtUtc);
        LastOccurredAtUtc = occurredUtc > LastOccurredAtUtc ? occurredUtc : LastOccurredAtUtc;
        OccurredAtUtc = LastOccurredAtUtc;
        OccurrenceCount += 1;
        LoggedAtUtc = DateTime.UtcNow;
        TraceId = traceId;

        if (!string.IsNullOrWhiteSpace(message))
            Message = message;

        if (!string.IsNullOrWhiteSpace(payloadJson))
            PayloadJson = payloadJson;

        if (IsSeverityHigher(severity, Severity))
            Severity = severity;
    }

    public void SetStatus(string status, string? actedBy, string? suppressionReason = null)
    {
        Status = status;

        if (status is ErrorLogStatuses.Resolved or ErrorLogStatuses.Suppressed)
        {
            ResolvedAtUtc = DateTime.UtcNow;
            ResolvedBy = actedBy;
            SuppressionReason = status == ErrorLogStatuses.Suppressed
                ? suppressionReason
                : null;
            return;
        }

        ResolvedAtUtc = null;
        ResolvedBy = null;
        SuppressionReason = null;
    }

    private static bool IsSeverityHigher(string candidate, string current)
    {
        return SeverityRank(candidate) > SeverityRank(current);
    }

    private static int SeverityRank(string severity)
    {
        return severity switch
        {
            "Critical" => 4,
            "Error" => 3,
            "Warning" => 2,
            _ => 1
        };
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
