using CrewService.Domain.Exceptions;
using Grpc.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CrewService.Infrastructure.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        _logger.LogError(exception, "Could not process a request on {MachineName}. TraceId: {TraceId}. DateTime: {DateTime}.",
            Environment.MachineName, traceId, DateTime.UtcNow);

        var (statusCode, title, extensions) = MapException(exception, traceId);

        await Results.Problem(
            title: title,
            statusCode: statusCode,
            extensions: extensions
        ).ExecuteAsync(httpContext);

        return true;
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
}
