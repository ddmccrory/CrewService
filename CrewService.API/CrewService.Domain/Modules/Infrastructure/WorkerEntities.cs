using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Infrastructure;

public sealed class WorkerSchedule : Entity
{
    public ControlNumber WorkAreaGroupCtrlNbr { get; private set; }
    public string WorkerType { get; private set; } = string.Empty;
    public string? CronExpression { get; private set; }
    public DateTime? NextFireUtc { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime? LastRunUtc { get; private set; }
    public string? LastRunStatus { get; private set; }

    private WorkerSchedule() { WorkAreaGroupCtrlNbr = null!; }

    public static WorkerSchedule Create(
        ControlNumber workAreaGroupCtrlNbr, string workerType,
        string? cronExpression = null, DateTime? nextFireUtc = null, bool isEnabled = true)
    {
        return new WorkerSchedule
        {
            WorkAreaGroupCtrlNbr = workAreaGroupCtrlNbr,
            WorkerType = workerType,
            CronExpression = cronExpression,
            NextFireUtc = nextFireUtc,
            IsEnabled = isEnabled
        };
    }

    public bool IsDue() => IsEnabled && NextFireUtc.HasValue && DateTime.UtcNow >= NextFireUtc.Value;

    public void RecordSuccess(DateTime? nextFire = null)
    {
        LastRunUtc = DateTime.UtcNow;
        LastRunStatus = "Success";
        NextFireUtc = nextFire;
    }

    public void RecordFailure(DateTime? nextFire = null)
    {
        LastRunUtc = DateTime.UtcNow;
        LastRunStatus = "Failed";
        NextFireUtc = nextFire;
    }
}

public sealed class WorkerExecutionLog : Entity
{
    public ControlNumber WorkerScheduleCtrlNbr { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string Status { get; private set; } = "Running";
    public string? ErrorMessage { get; private set; }

    private WorkerExecutionLog() { WorkerScheduleCtrlNbr = null!; }

    public static WorkerExecutionLog Start(ControlNumber workerScheduleCtrlNbr)
    {
        return new WorkerExecutionLog
        {
            WorkerScheduleCtrlNbr = workerScheduleCtrlNbr,
            StartedAtUtc = DateTime.UtcNow
        };
    }

    public void Complete()
    {
        CompletedAtUtc = DateTime.UtcNow;
        Status = "Success";
    }

    public void Fail(string errorMessage)
    {
        CompletedAtUtc = DateTime.UtcNow;
        Status = "Failed";
        ErrorMessage = errorMessage;
    }
}

public sealed class ProcessingLock
{
    public string LockKey { get; private set; } = string.Empty;
    public string AcquiredByInstance { get; private set; } = string.Empty;
    public DateTime AcquiredAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    private ProcessingLock() { }

    public static ProcessingLock Acquire(string lockKey, string instanceId, int expiryMinutes = 30)
    {
        return new ProcessingLock
        {
            LockKey = lockKey,
            AcquiredByInstance = instanceId,
            AcquiredAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };
    }

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAtUtc;

    public void Release() => ExpiresAtUtc = DateTime.UtcNow;
}
