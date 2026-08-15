using CrewService.Domain.Diagnostics;
using CrewService.Domain.Exceptions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace CrewService.Infrastructure.Exceptions;

public class GrpcExceptionInterceptor(
    ILogger<GrpcExceptionInterceptor> logger,
    IHttpContextAccessor httpContextAccessor,
    IErrorLogWriter errorLogWriter) : Interceptor
{
    private readonly ILogger<GrpcExceptionInterceptor> _logger = logger;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IErrorLogWriter _errorLogWriter = errorLogWriter;

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException rpcException)
        {
            await TryWriteRpcExceptionLogAsync(rpcException, context);
            throw;
        }
        catch (Exception ex)
        {
            throw await HandleExceptionAsync(ex, context);
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(requestStream, context);
        }
        catch (RpcException rpcException)
        {
            await TryWriteRpcExceptionLogAsync(rpcException, context);
            throw;
        }
        catch (Exception ex)
        {
            throw await HandleExceptionAsync(ex, context);
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (RpcException rpcException)
        {
            await TryWriteRpcExceptionLogAsync(rpcException, context);
            throw;
        }
        catch (Exception ex)
        {
            throw await HandleExceptionAsync(ex, context);
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        catch (RpcException rpcException)
        {
            await TryWriteRpcExceptionLogAsync(rpcException, context);
            throw;
        }
        catch (Exception ex)
        {
            throw await HandleExceptionAsync(ex, context);
        }
    }

    private async Task<RpcException> HandleExceptionAsync(Exception exception, ServerCallContext context)
    {
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogError(exception, "gRPC error on {MachineName}. TraceId: {TraceId}. Method: {Method}.",
            Environment.MachineName, traceId, context.Method);

        var (rpcException, errorCode) = MapException(exception, traceId);
        await TryWriteErrorLogAsync(
            exception,
            context,
            traceId,
            errorCode,
            ErrorLogKinds.UnhandledException,
            "Error",
            rpcException.StatusCode);
        return rpcException;
    }

    private async Task TryWriteRpcExceptionLogAsync(RpcException rpcException, ServerCallContext context)
    {
        var traceId = rpcException.Trailers.FirstOrDefault(t => t.Key == "trace-id")?.Value
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString();

        var errorCode = rpcException.Trailers.FirstOrDefault(t => t.Key == "code")?.Value
            ?? rpcException.StatusCode.ToString().ToUpperInvariant();

        await TryWriteErrorLogAsync(
            rpcException,
            context,
            traceId,
            errorCode,
            ErrorLogKinds.HandledFailure,
            ResolveSeverity(rpcException.StatusCode),
            rpcException.StatusCode);
    }

    private async Task TryWriteErrorLogAsync(
        Exception exception,
        ServerCallContext context,
        string traceId,
        string errorCode,
        string errorKind,
        string severity,
        StatusCode grpcStatusCode)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var parentCtrlNbr = TryParseHeaderAsLong(httpContext?.Request.Headers["x-parent-ctrl-nbr"].FirstOrDefault());
            var railroadCtrlNbr = TryParseHeaderAsLong(httpContext?.Request.Headers["x-railroad-ctrl-nbr"].FirstOrDefault());
            var performedBy = httpContext?.User.Identity?.Name ?? string.Empty;

            var payloadJson = JsonSerializer.Serialize(new
            {
                schemaVersion = "1.0",
                pipeline = "grpc-exception-interceptor",
                exceptionType = exception.GetType().FullName,
                message = exception.Message,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException?.ToString(),
                errorCode,
                errorKind,
                grpcStatusCode = grpcStatusCode.ToString(),
                grpcMethod = context.Method,
                host = httpContext?.Request.Host.Value,
                path = httpContext?.Request.Path.Value,
                timestampUtc = DateTime.UtcNow
            });

            await _errorLogWriter.WriteAsync(new ErrorLogWriteRequest(
                OccurredAtUtc: DateTime.UtcNow,
                ErrorKind: errorKind,
                SourceApp: "BackendApi",
                SourceLayer: "gRPC",
                Severity: severity,
                ErrorCode: errorCode,
                ExceptionType: exception.GetType().FullName ?? "Exception",
                Message: exception.Message,
                TraceId: traceId,
                Route: context.Method,
                Method: "gRPC",
                PerformedBy: performedBy,
                ParentCtrlNbr: parentCtrlNbr,
                RailroadCtrlNbr: railroadCtrlNbr,
                PayloadJson: payloadJson),
                context.CancellationToken);
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx, "Failed to persist gRPC error log. TraceId: {TraceId}", traceId);
        }
    }

    private static string ResolveSeverity(StatusCode statusCode)
    {
        return statusCode switch
        {
            StatusCode.Internal => "Critical",
            StatusCode.Unavailable => "Critical",
            StatusCode.DataLoss => "Critical",
            StatusCode.InvalidArgument => "Warning",
            StatusCode.NotFound => "Warning",
            StatusCode.FailedPrecondition => "Warning",
            _ => "Error"
        };
    }

    private static (RpcException RpcException, string ErrorCode) MapException(Exception exception, string traceId)
    {
        return exception switch
        {
            ValidationException validationEx => (new RpcException(
                new Status(StatusCode.InvalidArgument, validationEx.Message),
                CreateMetadata(validationEx.Code, traceId, validationEx.Errors)), validationEx.Code),

            NotFoundException notFoundEx => (new RpcException(
                new Status(StatusCode.NotFound, notFoundEx.Message),
                CreateMetadata(notFoundEx.Code, traceId)), notFoundEx.Code),

            ConflictException conflictEx => (new RpcException(
                new Status(StatusCode.AlreadyExists, conflictEx.Message),
                CreateMetadata(conflictEx.Code, traceId)), conflictEx.Code),

            ForbiddenException forbiddenEx => (new RpcException(
                new Status(StatusCode.PermissionDenied, forbiddenEx.Message),
                CreateMetadata(forbiddenEx.Code, traceId)), forbiddenEx.Code),

            DomainException domainEx => (new RpcException(
                new Status(StatusCode.InvalidArgument, domainEx.Message),
                CreateMetadata(domainEx.Code, traceId)), domainEx.Code),

            ArgumentNullException argNullEx => (new RpcException(
                new Status(StatusCode.InvalidArgument, argNullEx.Message),
                CreateMetadata("ARGUMENT_NULL", traceId)), "ARGUMENT_NULL"),

            _ => (new RpcException(
                new Status(StatusCode.Internal, "An unexpected error occurred."),
                CreateMetadata("INTERNAL_ERROR", traceId)), "INTERNAL_ERROR")
        };
    }

    private static long? TryParseHeaderAsLong(string? value)
    {
        return long.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : null;
    }

    private static Metadata CreateMetadata(string code, string traceId, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var metadata = new Metadata
        {
            { "code", code },
            { "trace-id", traceId }
        };

        if (errors is not null)
        {
            foreach (var error in errors)
            {
                metadata.Add($"error-{error.Key.ToLowerInvariant()}", string.Join("; ", error.Value));
            }
        }

        return metadata;
    }
}