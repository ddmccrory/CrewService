namespace CrewService.Application.BackgroundWorkers;

public interface IWaitListReassignmentSignal
{
    void Notify();
    void Notify(DateTime requestDateUtc);
    Task WaitAsync(CancellationToken ct);
    DateTime ConsumeRequestedDateUtcOrNow();
}

public sealed class WaitListReassignmentSignal : IWaitListReassignmentSignal
{
    private readonly SemaphoreSlim _gate = new(0, 1);
    private readonly object _lock = new();
    private DateTime? _requestedDateUtc;

    public void Notify() => Notify(DateTime.UtcNow);

    public void Notify(DateTime requestDateUtc)
    {
        var requestedDate = DateTime.SpecifyKind(requestDateUtc, DateTimeKind.Utc).Date;

        lock (_lock)
        {
            if (_requestedDateUtc is null || requestedDate < _requestedDateUtc.Value)
                _requestedDateUtc = requestedDate;

            if (_gate.CurrentCount == 0)
                _gate.Release();
        }
    }

    public async Task WaitAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
    }

    public DateTime ConsumeRequestedDateUtcOrNow()
    {
        lock (_lock)
        {
            var requestedDate = _requestedDateUtc;
            _requestedDateUtc = null;
            return requestedDate ?? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc).Date;
        }
    }
}
