namespace CrewService.Domain.Diagnostics;

public sealed record ErrorLogFilter(
    string? SearchText = null,
    DateTime? DateFromUtc = null,
    DateTime? DateToUtc = null,
    string? Severity = null,
    string? SourceApp = null,
    string? ErrorKind = null,
    string? Status = null,
    string? FingerprintHash = null,
    long? ParentCtrlNbr = null,
    long? RailroadCtrlNbr = null);

public sealed record ErrorLogWriteRequest(
    DateTime OccurredAtUtc,
    string ErrorKind,
    string SourceApp,
    string SourceLayer,
    string Severity,
    string ErrorCode,
    string ExceptionType,
    string Message,
    string TraceId,
    string? Route,
    string? Method,
    string PerformedBy,
    long? ParentCtrlNbr,
    long? RailroadCtrlNbr,
    string? PayloadJson,
    string? FingerprintHash = null,
    string Status = ErrorLogStatuses.New);

public interface IErrorLogQuery
{
    Task<(IReadOnlyList<ErrorLog> Entries, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        ErrorLogFilter? filter = null,
        CancellationToken ct = default);
}

public interface IErrorLogCommand
{
    Task<bool> UpdateStatusAsync(
        Guid errorId,
        string status,
        string actedBy,
        string? suppressionReason = null,
        CancellationToken ct = default);
}

public interface IErrorLogWriter
{
    Task WriteAsync(ErrorLogWriteRequest request, CancellationToken ct = default);
}
