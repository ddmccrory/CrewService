using System.Net.Http;

namespace CrewService.BlazorUI.Services;

public sealed class ApiHeartbeatService : IAppConnectivityService
{
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    private readonly ILogger<ApiHeartbeatService> _logger;
    private readonly Lock _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private ApiConnectivityStatus _status = ApiConnectivityStatus.Degraded;
    private DateTimeOffset? _lastSuccessUtc;
    private int _consecutiveConnectivityFailures;
    private readonly HttpClient _httpClient = new();

    public event Action? OnStatusChanged;

    public ApiHeartbeatService(IConfiguration configuration, ILogger<ApiHeartbeatService> logger)
    {
        _logger = logger;
        var baseAddress = configuration["CrewServiceApiUrl"]
            ?? throw new InvalidOperationException("CrewServiceApiUrl is not defined.");

        _httpClient.BaseAddress = new Uri(baseAddress, UriKind.Absolute);
    }

    public ApiConnectivityStatus Status
    {
        get
        {
            lock (_sync)
            {
                return _status;
            }
        }
    }

    public bool IsApiUnavailable => Status == ApiConnectivityStatus.Down;

    public DateTimeOffset? LastSuccessUtc
    {
        get
        {
            lock (_sync)
            {
                return _lastSuccessUtc;
            }
        }
    }

    public void EnsureStarted()
    {
        lock (_sync)
        {
            if (_loopTask is not null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
        }
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        await ProbeAsync(cancellationToken);

        using var timer = new PeriodicTimer(ProbeInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await ProbeAsync(cancellationToken);
        }
    }

    private async Task ProbeAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ProbeTimeout);

            using var request = new HttpRequestMessage(HttpMethod.Head, "/");
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

            SetConnectivity(ApiConnectivityStatus.Good, DateTimeOffset.UtcNow, resetFailures: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // normal shutdown
        }
        catch (OperationCanceledException)
        {
            var failures = IncrementConnectivityFailures();
            SetConnectivity(failures >= 1 ? ApiConnectivityStatus.Down : ApiConnectivityStatus.Degraded, null, resetFailures: false);
            _logger.LogWarning("API heartbeat probe timed out after {ProbeTimeoutSeconds} seconds.", ProbeTimeout.TotalSeconds);
        }
        catch (HttpRequestException ex)
        {
            var failures = IncrementConnectivityFailures();
            SetConnectivity(failures >= 1 ? ApiConnectivityStatus.Down : ApiConnectivityStatus.Degraded, null, resetFailures: false);
            _logger.LogWarning(ex, "API heartbeat probe failed due to connectivity issue.");
        }
        catch (Exception ex)
        {
            SetConnectivity(ApiConnectivityStatus.Degraded, null, resetFailures: true);
            _logger.LogWarning(ex, "API heartbeat probe returned a non-connectivity failure.");
        }
    }

    private int IncrementConnectivityFailures()
    {
        lock (_sync)
        {
            _consecutiveConnectivityFailures++;
            return _consecutiveConnectivityFailures;
        }
    }

    private void SetConnectivity(ApiConnectivityStatus nextStatus, DateTimeOffset? successUtc, bool resetFailures)
    {
        Action? callback;
        bool changed;
        lock (_sync)
        {
            if (resetFailures)
            {
                _consecutiveConnectivityFailures = 0;
            }

            if (successUtc.HasValue)
            {
                _lastSuccessUtc = successUtc.Value;
            }

            if (_status == nextStatus)
            {
                return;
            }

            _status = nextStatus;
            changed = true;
            callback = OnStatusChanged;
        }

        if (changed)
        {
            callback?.Invoke();
        }
    }

    public void Dispose()
    {
        CancellationTokenSource? cts;
        Task? loopTask;

        lock (_sync)
        {
            cts = _cts;
            loopTask = _loopTask;
            _cts = null;
            _loopTask = null;
        }

        if (cts is not null)
        {
            cts.Cancel();

            try
            {
                loopTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // expected on cancellation
            }
            finally
            {
                cts.Dispose();
            }
        }

        _httpClient.Dispose();
    }
}
