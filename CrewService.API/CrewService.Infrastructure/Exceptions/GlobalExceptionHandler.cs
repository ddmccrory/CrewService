using CrewService.Domain.Diagnostics;
using CrewService.Domain.Exceptions;
using Grpc.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace CrewService.Infrastructure.Exceptions;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IErrorLogWriter errorLogWriter) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly IErrorLogWriter _errorLogWriter = errorLogWriter;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        _logger.LogError(exception, "Could not process a request on {MachineName}. TraceId: {TraceId}. DateTime: {DateTime}.",
            Environment.MachineName, traceId, DateTime.UtcNow);

        var (statusCode, title, extensions) = MapException(exception, traceId);
        var errorCode = ResolveErrorCode(extensions, statusCode);
        var errorKind = ResolveErrorKind(exception);
        var severity = ResolveHttpSeverity(statusCode, exception);

        await TryWriteErrorLogAsync(httpContext, exception, traceId, errorCode, errorKind, severity, statusCode, cancellationToken);

        await Results.Problem(
            title: title,
            statusCode: statusCode,
            extensions: extensions
        ).ExecuteAsync(httpContext);

        return true;
    }

    private async Task TryWriteErrorLogAsync(
        HttpContext httpContext,
        Exception exception,
        string traceId,
        string errorCode,
        string errorKind,
        string severity,
        int statusCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var parentCtrlNbr = TryParseHeaderAsLong(httpContext.Request.Headers["x-parent-ctrl-nbr"].FirstOrDefault());
            var railroadCtrlNbr = TryParseHeaderAsLong(httpContext.Request.Headers["x-railroad-ctrl-nbr"].FirstOrDefault());
            var performedBy = httpContext.User.Identity?.Name ?? string.Empty;

            var payloadJson = JsonSerializer.Serialize(new
            {
                schemaVersion = "1.0",
                pipeline = "http-exception-handler",
                exceptionType = exception.GetType().FullName,
                message = exception.Message,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException?.ToString(),
                errorCode,
                errorKind,
                request = new
                {
                    scheme = httpContext.Request.Scheme,
                    method = httpContext.Request.Method,
                    path = httpContext.Request.Path.Value,
                    queryString = httpContext.Request.QueryString.Value,
                    host = httpContext.Request.Host.Value
                },
                response = new
                {
                    statusCode
                },
                timestampUtc = DateTime.UtcNow
            });

            await _errorLogWriter.WriteAsync(new ErrorLogWriteRequest(
                OccurredAtUtc: DateTime.UtcNow,
                ErrorKind: errorKind,
                SourceApp: "BackendApi",
                SourceLayer: "HTTP",
                Severity: severity,
                ErrorCode: errorCode,
                ExceptionType: exception.GetType().FullName ?? "Exception",
                Message: exception.Message,
                TraceId: traceId,
                Route: httpContext.Request.Path.Value,
                Method: httpContext.Request.Method,
                PerformedBy: performedBy,
                ParentCtrlNbr: parentCtrlNbr,
                RailroadCtrlNbr: railroadCtrlNbr,
                PayloadJson: payloadJson),
                cancellationToken);
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx, "Failed to persist HTTP error log. TraceId: {TraceId}", traceId);
        }
    }

    private static (int StatusCode, string Title, Dictionary<string, object?> Extensions) MapException(Exception exception, string traceId)
    {
        var extensions = new Dictionary<string, object?>
        {
            { "traceId", traceId }
        };

        return exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                validationEx.Message,
                new Dictionary<string, object?>
                {
                    { "traceId", traceId },
                    { "code", validationEx.Code },
                    { "errors", validationEx.Errors }
                }),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                notFoundEx.Message,
                new Dictionary<string, object?>
                {
                    { "traceId", traceId },
                    { "code", notFoundEx.Code },
                    { "entityName", notFoundEx.EntityName },
                    { "entityId", notFoundEx.EntityId }
                }),

            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                conflictEx.Message,
                new Dictionary<string, object?>
                {
                    { "traceId", traceId },
                    { "code", conflictEx.Code },
                    { "entityName", conflictEx.EntityName }
                }),

            ForbiddenException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                forbiddenEx.Message,
                new Dictionary<string, object?>
                {
                    { "traceId", traceId },
                    { "code", forbiddenEx.Code }
                }),

            DomainException domainEx => (
                StatusCodes.Status400BadRequest,
                domainEx.Message,
                new Dictionary<string, object?>
                {
                    { "traceId", traceId },
                    { "code", domainEx.Code }
                }),

            RpcException rpcEx => (
                (int)rpcEx.StatusCode,
                rpcEx.Message,
                extensions),

            ArgumentNullException argNullEx => (
                StatusCodes.Status400BadRequest,
                argNullEx.Message,
                extensions),

            AggregateException aggEx => HandleAggregateException(aggEx, traceId),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                extensions)
        };
    }

    private static (int StatusCode, string Title, Dictionary<string, object?> Extensions) HandleAggregateException(AggregateException aggEx, string traceId)
    {
        foreach (var innerEx in aggEx.InnerExceptions)
        {
            var result = MapException(innerEx, traceId);

            if (result.StatusCode != StatusCodes.Status500InternalServerError)
                return result;
        }

        return (
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            new Dictionary<string, object?> { { "traceId", traceId } });
    }

    private static string ResolveErrorCode(IReadOnlyDictionary<string, object?> extensions, int statusCode)
    {
        if (extensions.TryGetValue("code", out var codeObj) && codeObj is string code && !string.IsNullOrWhiteSpace(code))
            return code;

        return $"HTTP_{statusCode}";
    }

    private static long? TryParseHeaderAsLong(string? value)
    {
        return long.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : null;
    }

    private static string ResolveErrorKind(Exception exception)
    {
        return exception switch
        {
            ValidationException => ErrorLogKinds.Validation,
            DomainException => ErrorLogKinds.HandledFailure,
            RpcException => ErrorLogKinds.HandledFailure,
            _ => ErrorLogKinds.UnhandledException
        };
    }

    private static string ResolveHttpSeverity(int statusCode, Exception exception)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
            return "Critical";

        if (exception is ValidationException)
            return "Warning";

        return "Error";
    }
}
