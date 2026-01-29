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

        var (statusCode, title) = MapException(exception);

        await Results.Problem(
            title: title,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                { "traceId", traceId }
            }
        ).ExecuteAsync(httpContext);

        return true;
    }

    private static (int StatusCode, string Title) MapException(Exception exception)
    {
        return exception switch
        {
            RpcException rpcEx => ((int)rpcEx.StatusCode, rpcEx.Message),
            ArgumentNullException argNullEx => (StatusCodes.Status400BadRequest, argNullEx.Message),
            AggregateException aggEx => HandleAggregateException(aggEx),
            _ => InternalServerError()
        };
    }

    private static (int StatusCode, string Title) HandleAggregateException(AggregateException aggEx)
    {
        foreach (var innerEx in aggEx.InnerExceptions)
        {
            var result = MapException(innerEx);

            if (result.StatusCode != StatusCodes.Status500InternalServerError)
                return result;
        }

        return InternalServerError();
    }

    private static (int StatusCode, string Title) InternalServerError()
    {
        return (StatusCodes.Status500InternalServerError, "Internal Server Error");
    }
}
