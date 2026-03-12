using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.DomainEvents.Infrastructure;

public sealed record WorkerExecutionStartedDomainEvent : DomainEvent
{
    public WorkerExecutionStartedDomainEvent(ControlNumber scheduleCtrlNbr, string workerType)
        : base("WorkerSchedule", scheduleCtrlNbr.Value,
            payload: new { ScheduleCtrlNbr = scheduleCtrlNbr.Value, WorkerType = workerType }) { }
}

public sealed record WorkerExecutionCompletedDomainEvent : DomainEvent
{
    public WorkerExecutionCompletedDomainEvent(ControlNumber logCtrlNbr, string workerType, string status)
        : base("WorkerExecutionLog", logCtrlNbr.Value,
            payload: new { LogCtrlNbr = logCtrlNbr.Value, WorkerType = workerType, Status = status }) { }
}

public sealed record WorkerExecutionFailedDomainEvent : DomainEvent
{
    public WorkerExecutionFailedDomainEvent(ControlNumber logCtrlNbr, string workerType, string errorMessage)
        : base("WorkerExecutionLog", logCtrlNbr.Value,
            payload: new { LogCtrlNbr = logCtrlNbr.Value, WorkerType = workerType, ErrorMessage = errorMessage }) { }
}

public sealed record ProcessingLockConflictDomainEvent : DomainEvent
{
    public ProcessingLockConflictDomainEvent(string lockKey, string blockedInstance)
        : base("ProcessingLock", 0,
            payload: new { LockKey = lockKey, BlockedInstance = blockedInstance }) { }
}
