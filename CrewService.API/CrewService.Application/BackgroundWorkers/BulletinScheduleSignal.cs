namespace CrewService.Application.BackgroundWorkers;

using System.Threading;

/// <summary>
/// Push signal that lets the bulletin creation / NoBid paths notify the
/// <c>BulletinProcessingWorker</c> of the exact UTC time it should next wake.
/// </summary>
public interface IBulletinScheduleSignal
{
    /// <summary>
    /// Records <paramref name="eventUtc"/> as a pending bulletin event.
    /// If this time is earlier than the currently stored next-event time the worker
    /// is woken immediately so it can recalculate its sleep duration.
    /// </summary>
    void Notify(DateTime eventUtc);

    /// <summary>
    /// Suspends the caller until the earliest known bulletin event time is reached,
    /// or until a new <see cref="Notify"/> call arrives with an earlier time.
    /// Returns immediately when the event time has already passed.
    /// </summary>
    Task WaitAsync(CancellationToken ct);
}

/// <summary>
/// Thread-safe singleton implementation of <see cref="IBulletinScheduleSignal"/>.
/// </summary>
public sealed class BulletinScheduleSignal : IBulletinScheduleSignal
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
                // Wake the worker so it recalculates its sleep duration with the new (earlier) time.
                // SemaphoreSlim.Release throws if count would exceed max; only release if at zero.
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
                // No event known yet — wait until Notify is called.
                await _gate.WaitAsync(ct);
                continue;
            }

            if (target.Value <= now)
            {
                // Event time has arrived — clear it and return so the worker runs.
                lock (_lock)
                {
                    // Only clear if it hasn't been updated to a later time by another Notify.
                    if (_nextEventUtc.HasValue && _nextEventUtc.Value <= now)
                        _nextEventUtc = null;
                }
                // Drain the semaphore if a concurrent Notify released it.
                _gate.Wait(0, CancellationToken.None);
                return;
            }

            var delay = target.Value - now;
            // Wait until the event fires OR a new (earlier) Notify wakes us.
            var signaled = await _gate.WaitAsync((int)Math.Min(delay.TotalMilliseconds, int.MaxValue), ct);
            if (!signaled)
            {
                // Timeout — the target time has arrived.
                lock (_lock)
                {
                    if (_nextEventUtc.HasValue && _nextEventUtc.Value <= DateTime.UtcNow)
                        _nextEventUtc = null;
                }
                return;
            }
            // Signaled by Notify — loop back and re-evaluate with the new target time.
        }
    }
}
