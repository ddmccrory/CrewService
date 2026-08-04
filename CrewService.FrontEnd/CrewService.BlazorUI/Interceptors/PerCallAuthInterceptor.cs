using CrewService.BlazorUI.Services;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CrewService.BlazorUI.Interceptors;

/// <summary>
/// gRPC client interceptor that reads the bearer token from <see cref="CircuitTokenProvider"/>
/// on every call, ensuring the current token is always used.
/// Also sends selected context values as metadata headers.
/// </summary>
public sealed class PerCallAuthInterceptor(CircuitTokenProvider tokenProvider, AppContextService appContext) : Interceptor
{
    private readonly CircuitTokenProvider _tokenProvider = tokenProvider;
    private readonly AppContextService _appContext = appContext;

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = context.Options.Headers ?? new Metadata();
        var token = _tokenProvider.AccessToken;

        if (!string.IsNullOrEmpty(token))
            headers.Add("Authorization", $"Bearer {token}");

        if (_appContext.SelectedParentCtrlNbr.HasValue)
            headers.Add("x-parent-ctrl-nbr", _appContext.SelectedParentCtrlNbr.Value.ToString());

        if (_appContext.SelectedRailroadCtrlNbr.HasValue)
            headers.Add("x-railroad-ctrl-nbr", _appContext.SelectedRailroadCtrlNbr.Value.ToString());

        var newOptions = context.Options.WithHeaders(headers);
        context = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);

        return base.AsyncUnaryCall(request, context, continuation);
    }
}
