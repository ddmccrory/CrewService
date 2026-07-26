namespace CrewService.Application.BackgroundWorkers;

public interface IWaitListReassignmentSignal
{
    void Notify();
    Task WaitAsync(CancellationToken ct);
}

public sealed class WaitListReassignmentSignal : IWaitListReassignmentSignal
{
    private readonly SemaphoreSlim _gate = new(0, 1);

    public void Notify()
    {
        if (_gate.CurrentCount == 0)
            _gate.Release();
    }

    public async Task WaitAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
    }
}
