using CrewService.BlazorUI.Interceptors;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CrewService.BlazorUI.Clients;

public abstract class BaseGrpcClient<TClient>
{
    protected readonly TClient _client;
    protected readonly ILogger _logger;

    protected BaseGrpcClient(GrpcChannelProvider channelProvider, IHttpContextAccessor httpContextAccessor, Func<CallInvoker, TClient> clientFactory, ILogger logger, bool addAuthHeader = true)
    {
        var channel = channelProvider.Channel;

        CallInvoker callInvoker;

        if (addAuthHeader)
        {
            var token = httpContextAccessor.HttpContext?.User.FindFirst("AccessToken")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                callInvoker = channel.Intercept(new AuthInterceptor(token));
            }
            else
            {
                // Allow construction to succeed — unauthenticated users will be
                // redirected by [Authorize] before any gRPC call is made.
                callInvoker = channel.CreateCallInvoker();
            }
        }
        else
        {
            callInvoker = channel.CreateCallInvoker();
        }

        _client = clientFactory(callInvoker);
        _logger = logger;
    }

    protected void LogException(Exception ex)
    {
        if (ex is RpcException rpcEx)
        {
            _logger.LogError(ex, "gRPC error occurred. Detail: {Detail}", rpcEx.Status.Detail);
        }
        else
        {
            _logger.LogError(ex, "An unexpected error occurred. Message: {Message}", ex.Message);
        }
    }
}
