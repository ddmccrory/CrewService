using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CrewService.BlazorUI.Interceptors;

public class AuthInterceptor(string token) : Interceptor
{
    private readonly string _token = token;

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = new Metadata
        {
            { "Authorization", $"Bearer {_token}" }
        };

        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, context.Options.WithHeaders(headers));

        return base.AsyncUnaryCall(request, newContext, continuation);
    }
}
