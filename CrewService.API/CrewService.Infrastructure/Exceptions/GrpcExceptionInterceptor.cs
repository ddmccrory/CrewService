using CrewService.Domain.Exceptions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CrewService.Infrastructure.Exceptions;

public class GrpcExceptionInterceptor(ILogger<GrpcExceptionInterceptor> logger) : Interceptor
{
    private readonly ILogger<GrpcExceptionInterceptor> _logger = logger;

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw; // Already an RpcException, let it pass through
        }
        catch (Exception ex)
        {
            throw HandleException(ex, context);
        }
    }

    private RpcException HandleException(Exception exception, ServerCallContext context)
    {
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogError(exception, "gRPC error on {MachineName}. TraceId: {TraceId}. Method: {Method}.",
            Environment.MachineName, traceId, context.Method);

        return exception switch
        {
            ValidationException validationEx => new RpcException(
                new Status(StatusCode.InvalidArgument, validationEx.Message),
                CreateMetadata(validationEx.Code, traceId, validationEx.Errors)),

            NotFoundException notFoundEx => new RpcException(
                new Status(StatusCode.NotFound, notFoundEx.Message),
                CreateMetadata(notFoundEx.Code, traceId)),

            ConflictException conflictEx => new RpcException(
                new Status(StatusCode.AlreadyExists, conflictEx.Message),
                CreateMetadata(conflictEx.Code, traceId)),

            ForbiddenException forbiddenEx => new RpcException(
                new Status(StatusCode.PermissionDenied, forbiddenEx.Message),
                CreateMetadata(forbiddenEx.Code, traceId)),

            DomainException domainEx => new RpcException(
                new Status(StatusCode.InvalidArgument, domainEx.Message),
                CreateMetadata(domainEx.Code, traceId)),

            ArgumentNullException argNullEx => new RpcException(
                new Status(StatusCode.InvalidArgument, argNullEx.Message),
                CreateMetadata("ARGUMENT_NULL", traceId)),

            _ => new RpcException(
                new Status(StatusCode.Internal, "An unexpected error occurred."),
                CreateMetadata("INTERNAL_ERROR", traceId))
        };
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