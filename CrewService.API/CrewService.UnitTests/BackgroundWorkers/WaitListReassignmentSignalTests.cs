using CrewService.Application.BackgroundWorkers;
using Xunit;

namespace CrewService.UnitTests.BackgroundWorkers;

public sealed class WaitListReassignmentSignalTests
{
    [Fact]
    public async Task Notify_WithRequestedDate_WakesWaiters_AndReturnsRequestedDate()
    {
        var signal = new WaitListReassignmentSignal();
        var requestedDateUtc = new DateTime(2026, 7, 28, 14, 30, 0, DateTimeKind.Utc);

        signal.Notify(requestedDateUtc);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await signal.WaitAsync(cts.Token);

        var targetDateUtc = signal.ConsumeRequestedDateUtcOrNow();
        Assert.Equal(new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc), targetDateUtc);
    }

    [Fact]
    public async Task Notify_MultipleDates_PrefersEarliestRequestedDate()
    {
        var signal = new WaitListReassignmentSignal();

        signal.Notify(new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc));
        signal.Notify(new DateTime(2026, 7, 28, 16, 0, 0, DateTimeKind.Utc));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await signal.WaitAsync(cts.Token);

        var targetDateUtc = signal.ConsumeRequestedDateUtcOrNow();
        Assert.Equal(new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc), targetDateUtc);
    }
}
