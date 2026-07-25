namespace CrewService.Application.BackgroundWorkers;

/// <summary>
/// Wakes the <see cref="Workers.AutoMarkUpWorker"/> when an open absence has a scheduled end
/// or when a newly scheduled/updated end creates an earlier due time.
/// </summary>
public interface IAutoMarkUpSignal
{
    void Notify(DateTime eventUtc);
    Task WaitAsync(CancellationToken ct);
}

/// <summary>
/// Thread-safe singleton signal for scheduling auto mark-up processing at the earliest due UTC time.
/// </summary>
public sealed class AutoMarkUpSignal : IAutoMarkUpSignal
{
    private readonly SemaphoreSlim _gate = new(0, 1);
    private DateTime? _nextEventUtc;
    private readonly object _lock = new();

    public void Notify(DateTime eventUtc)
    {
        lock (_lock)
        {
            if (_nextEventUtc is null || eventUtc < _nextEventUtc.Value)
            {
                _nextEventUtc = eventUtc;
                if (_gate.CurrentCount == 0)
                    _gate.Release();
            }
        }
    }

    public async Task WaitAsync(CancellationToken ct)
    {
        while (true)
        {
            DateTime? next;
            lock (_lock) { next = _nextEventUtc; }

            if (next is null)
            {
                await _gate.WaitAsync(ct);
                continue;
            }

            var delay = next.Value - DateTime.UtcNow;
            if (delay <= TimeSpan.Zero)
            {
                lock (_lock) { _nextEventUtc = null; }
                return;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(delay);
            try
            {
                await _gate.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                lock (_lock) { _nextEventUtc = null; }
                return;
            }
        }
    }
}
