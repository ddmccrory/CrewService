namespace CrewService.BlazorUI.Services;

/// <summary>
/// Scoped service that captures the JWT access token from HttpContext during
/// circuit/scope creation and makes it available for per-call gRPC authorization.
/// </summary>
public sealed class CircuitTokenProvider(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>
    /// The bearer token for gRPC calls. Captured from HttpContext at scope creation;
    /// may be updated later (e.g., token refresh).
    /// </summary>
    public string? AccessToken { get; set; }
        = httpContextAccessor.HttpContext?.User.FindFirst("AccessToken")?.Value;
}
