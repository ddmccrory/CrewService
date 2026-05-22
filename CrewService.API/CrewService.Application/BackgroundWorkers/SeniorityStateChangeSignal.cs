namespace CrewService.Application.BackgroundWorkers;

/// <summary>
/// Identical in purpose to <see cref="IBulletinScheduleSignal"/> but drives
/// the <see cref="SeniorityStateChangeWorker"/> instead.
/// </summary>
public interface ISeniorityStateChangeSignal
{
    void Notify(DateTime eventUtc);
    Task WaitAsync(CancellationToken ct);
}

/// <summary>
/// Thread-safe singleton that wakes the <see cref="SeniorityStateChangeWorker"/>
/// at exactly the next pending state-change effective date.
/// </summary>
public sealed class SeniorityStateChangeSignal : ISeniorityStateChangeSignal
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
            DateTime? target;
            lock (_lock) { target = _nextEventUtc; }

            var now = DateTime.UtcNow;

            if (target is null)
            {
                await _gate.WaitAsync(ct);
                continue;
            }

            if (target.Value <= now)
            {
                lock (_lock)
                {
                    if (_nextEventUtc.HasValue && _nextEventUtc.Value <= now)
                        _nextEventUtc = null;
                }
                _gate.Wait(0);
                return;
            }

            var delay = target.Value - now;
            var signaled = await _gate.WaitAsync((int)Math.Min(delay.TotalMilliseconds, int.MaxValue), ct);
            if (!signaled)
            {
                lock (_lock)
                {
                    if (_nextEventUtc.HasValue && _nextEventUtc.Value <= DateTime.UtcNow)
                        _nextEventUtc = null;
                }
                return;
            }
        }
    }
}
