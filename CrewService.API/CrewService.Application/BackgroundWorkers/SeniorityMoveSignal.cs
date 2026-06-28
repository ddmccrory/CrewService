namespace CrewService.Application.BackgroundWorkers;

/// <summary>
/// Wakes the <see cref="Workers.SeniorityMoveWorker"/> when a seniority move is approved
/// or when the next move's effective time is reached.
/// </summary>
public interface ISeniorityMoveSignal
{
    void Notify(DateTime eventUtc);
    Task WaitAsync(CancellationToken ct);
}

/// <summary>
/// Thread-safe singleton that wakes the <see cref="Workers.SeniorityMoveWorker"/>
/// at exactly the next approved move's effective date.
/// </summary>
public sealed class SeniorityMoveSignal : ISeniorityMoveSignal
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
