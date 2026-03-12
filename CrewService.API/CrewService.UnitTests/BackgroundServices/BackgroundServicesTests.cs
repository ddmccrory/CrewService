using CrewService.Domain.Modules.Infrastructure;
using CrewService.Domain.ValueObjects;
using Xunit;

namespace CrewService.UnitTests.BackgroundServices;

public class WorkerScheduleTests
{
    [Fact]
    public void IsDue_EnabledAndPastNextFire_ReturnsTrue()
    {
        var schedule = WorkerSchedule.Create(
            ControlNumber.Create(1), "CallSheet",
            nextFireUtc: DateTime.UtcNow.AddMinutes(-1));
        Assert.True(schedule.IsDue());
    }

    [Fact]
    public void IsDue_Disabled_ReturnsFalse()
    {
        var schedule = WorkerSchedule.Create(
            ControlNumber.Create(1), "CallSheet",
            nextFireUtc: DateTime.UtcNow.AddMinutes(-1), isEnabled: false);
        Assert.False(schedule.IsDue());
    }

    [Fact]
    public void IsDue_FutureNextFire_ReturnsFalse()
    {
        var schedule = WorkerSchedule.Create(
            ControlNumber.Create(1), "CallSheet",
            nextFireUtc: DateTime.UtcNow.AddHours(1));
        Assert.False(schedule.IsDue());
    }

    [Fact]
    public void RecordSuccess_SetsStatusAndNextFire()
    {
        var schedule = WorkerSchedule.Create(
            ControlNumber.Create(1), "CallSheet",
            nextFireUtc: DateTime.UtcNow);
        var nextFire = DateTime.UtcNow.AddHours(1);
        schedule.RecordSuccess(nextFire);

        Assert.Equal("Success", schedule.LastRunStatus);
        Assert.Equal(nextFire, schedule.NextFireUtc);
        Assert.NotNull(schedule.LastRunUtc);
    }

    [Fact]
    public void RecordFailure_SetsStatusToFailed()
    {
        var schedule = WorkerSchedule.Create(
            ControlNumber.Create(1), "CallSheet",
            nextFireUtc: DateTime.UtcNow);
        schedule.RecordFailure();

        Assert.Equal("Failed", schedule.LastRunStatus);
    }
}

public class WorkerExecutionLogTests
{
    [Fact]
    public void Start_SetsRunningStatus()
    {
        var log = WorkerExecutionLog.Start(ControlNumber.Create(1));
        Assert.Equal("Running", log.Status);
        Assert.Null(log.CompletedAtUtc);
    }

    [Fact]
    public void Complete_SetsSuccessAndTimestamp()
    {
        var log = WorkerExecutionLog.Start(ControlNumber.Create(1));
        log.Complete();
        Assert.Equal("Success", log.Status);
        Assert.NotNull(log.CompletedAtUtc);
    }

    [Fact]
    public void Fail_SetsFailedWithMessage()
    {
        var log = WorkerExecutionLog.Start(ControlNumber.Create(1));
        log.Fail("Connection timeout");
        Assert.Equal("Failed", log.Status);
        Assert.Equal("Connection timeout", log.ErrorMessage);
    }
}

public class ProcessingLockTests
{
    [Fact]
    public void Acquire_SetsLockKey()
    {
        var processingLock = ProcessingLock.Acquire("CallSheet:1", "HOST-A", 30);
        Assert.Equal("CallSheet:1", processingLock.LockKey);
        Assert.Equal("HOST-A", processingLock.AcquiredByInstance);
        Assert.False(processingLock.IsExpired());
    }

    [Fact]
    public void IsExpired_PastExpiry_ReturnsTrue()
    {
        var processingLock = ProcessingLock.Acquire("Test:1", "HOST-A", 0);
        Assert.True(processingLock.IsExpired());
    }

    [Fact]
    public void Release_MakesExpired()
    {
        var processingLock = ProcessingLock.Acquire("Test:1", "HOST-A", 30);
        processingLock.Release();
        Assert.True(processingLock.IsExpired());
    }
}
