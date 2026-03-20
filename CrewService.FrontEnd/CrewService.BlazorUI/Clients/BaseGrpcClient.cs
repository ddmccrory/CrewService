using CrewService.BlazorUI.Interceptors;
using CrewService.BlazorUI.Services;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CrewService.BlazorUI.Clients;

public abstract class BaseGrpcClient<TClient>
{
    protected readonly TClient _client;
    protected readonly ILogger _logger;

    protected BaseGrpcClient(GrpcChannelProvider channelProvider, CircuitTokenProvider tokenProvider, Func<CallInvoker, TClient> clientFactory, ILogger logger, bool addAuthHeader = true)
    {
        var channel = channelProvider.Channel;

        CallInvoker callInvoker = addAuthHeader
            ? channel.Intercept(new PerCallAuthInterceptor(tokenProvider))
            : channel.CreateCallInvoker();

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
