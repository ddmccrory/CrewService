using CrewService.Domain.Diagnostics;
using System.Diagnostics;
using System.Text.Json;

namespace CrewService.Infrastructure.Diagnostics;

internal sealed class HandledFailureLogger(IErrorLogWriter errorLogWriter) : IHandledFailureLogger
{
    public async Task LogAsync(HandledFailureLogRequest request, CancellationToken ct = default)
    {
        var traceId = request.TraceId
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString();

        var exceptionType = request.ExceptionType
            ?? request.Exception?.GetType().FullName
            ?? "HandledFailure";

        var message = string.IsNullOrWhiteSpace(request.Message)
            ? request.Exception?.Message ?? "Handled failure detected."
            : request.Message;

        var payloadJson = request.PayloadJson;
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            payloadJson = JsonSerializer.Serialize(new
            {
                schemaVersion = "1.0",
                pipeline = "handled-failure-logger",
                component = request.Component,
                operation = request.Operation,
                errorCode = request.ErrorCode,
                message,
                exceptionType,
                exception = request.Exception?.ToString(),
                timestampUtc = DateTime.UtcNow
            });
        }

        await errorLogWriter.WriteAsync(new ErrorLogWriteRequest(
            OccurredAtUtc: DateTime.UtcNow,
            ErrorKind: ErrorLogKinds.HandledFailure,
            SourceApp: "BackendApi",
            SourceLayer: request.SourceLayer,
            Severity: request.Severity,
            ErrorCode: request.ErrorCode,
            ExceptionType: exceptionType,
            Message: message,
            TraceId: traceId,
            Route: request.Route,
            Method: request.Method,
            PerformedBy: request.PerformedBy ?? string.Empty,
            ParentCtrlNbr: request.ParentCtrlNbr,
            RailroadCtrlNbr: request.RailroadCtrlNbr,
            PayloadJson: payloadJson,
            FingerprintHash: request.FingerprintHash,
            Status: request.Status),
            ct);
    }
}
