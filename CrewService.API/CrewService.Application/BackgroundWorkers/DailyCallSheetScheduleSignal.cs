namespace CrewService.Application.BackgroundWorkers;

using System.Threading;

/// <summary>
/// Push signal that lets daily operations paths notify the
/// <c>DailyCallSheetWorker</c> of the exact UTC time it should next wake.
/// </summary>
public interface IDailyCallSheetScheduleSignal
{
    /// <summary>
    /// Records <paramref name="eventUtc"/> as a pending call-sheet event.
    /// If this time is earlier than the currently stored next-event time the worker
    /// is woken immediately so it can recalculate its sleep duration.
    /// </summary>
    void Notify(DateTime eventUtc);

    /// <summary>
    /// Suspends the caller until the earliest known call-sheet event time is reached,
    /// or until a new <see cref="Notify"/> call arrives with an earlier time.
    /// Returns immediately when the event time has already passed.
    /// </summary>
    Task WaitAsync(CancellationToken ct);
}

/// <summary>
/// Thread-safe singleton implementation of <see cref="IDailyCallSheetScheduleSignal"/>.
/// </summary>
public sealed class DailyCallSheetScheduleSignal : IDailyCallSheetScheduleSignal
{
    private readonly SemaphoreSlim _gate = new(0, 1);
    private DateTime? _nextEventUtc;
    private readonly Lock _lock = new();

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
            lock (_lock)
            {
                target = _nextEventUtc;
            }

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
                _gate.Wait(0, CancellationToken.None);
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
