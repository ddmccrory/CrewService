namespace CrewService.Domain.Diagnostics;

public sealed record HandledFailureLogRequest(
    string SourceLayer,
    string Component,
    string Operation,
    string ErrorCode,
    string Message,
    string Severity = "Error",
    string? ExceptionType = null,
    Exception? Exception = null,
    string? TraceId = null,
    string? Route = null,
    string? Method = null,
    string? PerformedBy = null,
    long? ParentCtrlNbr = null,
    long? RailroadCtrlNbr = null,
    string? PayloadJson = null,
    string? FingerprintHash = null,
    string Status = ErrorLogStatuses.New);

public interface IHandledFailureLogger
{
    Task LogAsync(HandledFailureLogRequest request, CancellationToken ct = default);
}
