using CrewService.BlazorUI.Services;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CrewService.BlazorUI.Interceptors;

/// <summary>
/// gRPC client interceptor that reads the bearer token from <see cref="CircuitTokenProvider"/>
/// on every call, ensuring the current token is always used.
/// </summary>
public sealed class PerCallAuthInterceptor(CircuitTokenProvider tokenProvider) : Interceptor
{
    private readonly CircuitTokenProvider _tokenProvider = tokenProvider;

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var token = _tokenProvider.AccessToken;

        if (!string.IsNullOrEmpty(token))
        {
            var headers = context.Options.Headers ?? new Metadata();
            headers.Add("Authorization", $"Bearer {token}");

            var newOptions = context.Options.WithHeaders(headers);
            context = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, newOptions);
        }

        return base.AsyncUnaryCall(request, context, continuation);
    }
}
