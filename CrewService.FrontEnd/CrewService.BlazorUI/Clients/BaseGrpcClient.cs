using CrewService.BlazorUI.Interceptors;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;

namespace CrewService.BlazorUI.Clients;

public abstract class BaseGrpcClient<TClient>
{
    protected readonly TClient _client;
    protected readonly ILogger _logger;

    protected BaseGrpcClient(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, Func<CallInvoker, TClient> clientFactory, ILogger logger, bool addAuthHeader = true)
    {
        var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler());

        var baseAddress = configuration["CrewServiceApiUrl"] ??
            throw new Exception("CrewServiceApiUrl is not defined.");

        var channel = GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions { HttpHandler = httpHandler });

        CallInvoker callInvoker;

        if (addAuthHeader)
        {
            // Extract the token from claims
            var token = httpContextAccessor.HttpContext?.User.FindFirst("AccessToken")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Access token is not available.");
            }

            // Create the interceptor
            callInvoker = channel.Intercept(new AuthInterceptor(token));
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
