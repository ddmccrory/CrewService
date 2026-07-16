namespace CrewService.BlazorUI.Services;

public interface IAppConnectivityService : IDisposable
{
    event Action? OnStatusChanged;

    ApiConnectivityStatus Status { get; }
    bool IsApiUnavailable { get; }
    DateTimeOffset? LastSuccessUtc { get; }

    void EnsureStarted();
}
