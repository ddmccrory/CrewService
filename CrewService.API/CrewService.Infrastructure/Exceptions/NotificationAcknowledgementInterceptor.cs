using CrewService.Application.Notifications;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace CrewService.Infrastructure.Exceptions;

public sealed class NotificationAcknowledgementInterceptor(
    INotificationAcknowledgementEnforcer enforcer,
    ILogger<NotificationAcknowledgementInterceptor> logger)
    : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var openCount = await enforcer.GetBlockingOpenCountAsync(context.Method, context.CancellationToken);
        if (openCount > 0)
        {
            logger.LogWarning(
                "Notification acknowledgement guard blocked gRPC method {GrpcMethod} with {OpenCount} open required notices.",
                context.Method,
                openCount);

            var metadata = new Metadata
            {
                { "code", "NOTIFICATION_ACKNOWLEDGEMENT_REQUIRED" },
                { "open-count", openCount.ToString() }
            };

            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    "You must acknowledge required notifications before proceeding."),
                metadata);
        }

        return await continuation(request, context);
    }
}
