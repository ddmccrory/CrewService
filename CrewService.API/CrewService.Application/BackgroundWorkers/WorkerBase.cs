using CrewService.Domain.Modules.Infrastructure;
using CrewService.Domain.Interfaces;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.BackgroundWorkers;

public interface IWorkerScheduleRepository
{
    Task<IReadOnlyList<WorkerSchedule>> GetDueByTypeAsync(string workerType, CancellationToken ct = default);
    Task<IReadOnlyList<WorkerSchedule>> GetEnabledByTypeAsync(string workerType, CancellationToken ct = default);
    Task<IReadOnlyList<WorkerSchedule>> GetAllAsync(string? workerType = null, CancellationToken ct = default);
    Task<WorkerSchedule?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default);
    Task AddAsync(WorkerSchedule schedule, CancellationToken ct = default);
    Task UpdateAsync(WorkerSchedule schedule, CancellationToken ct = default);
}

public interface IWorkerExecutionLogRepository
{
    Task<IReadOnlyList<WorkerExecutionLog>> GetByScheduleAsync(ControlNumber workerScheduleCtrlNbr, int limit = 20, CancellationToken ct = default);
    Task AddAsync(WorkerExecutionLog log, CancellationToken ct = default);
    Task UpdateAsync(WorkerExecutionLog log, CancellationToken ct = default);
}

public interface IProcessingLockService
{
    Task<ProcessingLock?> TryAcquireAsync(string lockKey, string instanceId, int expiryMinutes = 30, CancellationToken ct = default);
    Task ReleaseAsync(string lockKey, CancellationToken ct = default);
}

public interface IWorkerHeartbeatRegistry
{
    DateTime? GetLastHeartbeatUtc(ControlNumber scheduleCtrlNbr);
    void RecordHeartbeat(ControlNumber scheduleCtrlNbr, DateTime utcNow);
}

public sealed class WorkerHeartbeatRegistry : IWorkerHeartbeatRegistry
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, DateTime> _heartbeats = new();

    public DateTime? GetLastHeartbeatUtc(ControlNumber scheduleCtrlNbr)
    {
        return _heartbeats.TryGetValue(scheduleCtrlNbr.Value, out var heartbeat)
            ? heartbeat
            : null;
    }

    public void RecordHeartbeat(ControlNumber scheduleCtrlNbr, DateTime utcNow)
    {
        _heartbeats[scheduleCtrlNbr.Value] = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }
}

public abstract class WorkerBase(
    IServiceScopeFactory scopeFactory,
    ILogger logger,
    string workerType,
    TimeSpan checkInterval) : BackgroundService
{
    protected IServiceScopeFactory ScopeFactory { get; } = scopeFactory;
    protected virtual bool UseDueScheduleGate => true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!UseDueScheduleGate)
                {
                    using var signalScope = ScopeFactory.CreateScope();
                    var signalScheduleRepo = signalScope.ServiceProvider.GetRequiredService<IWorkerScheduleRepository>();
                    var signalHeartbeatRegistry = signalScope.ServiceProvider.GetRequiredService<IWorkerHeartbeatRegistry>();
                    var signalEnabledSchedules = await signalScheduleRepo.GetEnabledByTypeAsync(workerType, stoppingToken);
                    foreach (var enabledSchedule in signalEnabledSchedules)
                        signalHeartbeatRegistry.RecordHeartbeat(enabledSchedule.CtrlNbr, DateTime.UtcNow);

                    await WaitForNextRunAsync(stoppingToken);
                }

                using var scope = ScopeFactory.CreateScope();
                var currentUserService = scope.ServiceProvider.GetService<ICurrentUserService>();
                currentUserService?.SetAuditOverride($"{workerType}Worker");

                var scheduleRepo = scope.ServiceProvider.GetRequiredService<IWorkerScheduleRepository>();
                var logRepo = scope.ServiceProvider.GetRequiredService<IWorkerExecutionLogRepository>();
                var lockService = scope.ServiceProvider.GetRequiredService<IProcessingLockService>();
                var heartbeatRegistry = scope.ServiceProvider.GetRequiredService<IWorkerHeartbeatRegistry>();

                var dueSchedules = UseDueScheduleGate
                    ? await scheduleRepo.GetDueByTypeAsync(workerType, stoppingToken)
                    : await scheduleRepo.GetEnabledByTypeAsync(workerType, stoppingToken);

                foreach (var schedule in dueSchedules)
                {
                    heartbeatRegistry.RecordHeartbeat(schedule.CtrlNbr, DateTime.UtcNow);

                    var lockKey = $"{workerType}:{schedule.WorkAreaGroupCtrlNbr.Value}";
                    var processingLock = await lockService.TryAcquireAsync(lockKey, Environment.MachineName, ct: stoppingToken);

                    if (processingLock is null)
                    {
                        if (logger.IsEnabled(LogLevel.Information))
                            logger.LogInformation("Lock {LockKey} held by another instance, skipping", lockKey);
                        continue;
                    }

                    try
                    {
                        var executionLog = WorkerExecutionLog.Start(schedule.CtrlNbr);
                        var executionLogPersisted = false;

                        try
                        {
                            await logRepo.AddAsync(executionLog, stoppingToken);
                            executionLogPersisted = true;

                            var didWork = await ExecuteWorkAsync(scope.ServiceProvider, schedule, stoppingToken);
                            executionLog.Complete(didWork);
                            if (didWork)
                                schedule.RecordSuccess(CalculateNextFire(schedule));
                        }
                        catch (Exception ex)
                        {
                            if (executionLogPersisted)
                                executionLog.Fail(ex.Message);

                            schedule.RecordFailure(CalculateNextFire(schedule));
                            logger.LogError(ex, "Worker {WorkerType} failed for schedule {ScheduleCtrlNbr}", workerType, schedule.CtrlNbr.Value);
                        }
                        finally
                        {
                            if (executionLogPersisted)
                                await logRepo.UpdateAsync(executionLog, stoppingToken);

                            await scheduleRepo.UpdateAsync(schedule, stoppingToken);
                        }
                    }
                    finally
                    {
                        await lockService.ReleaseAsync(lockKey, stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Worker {WorkerType} encountered an unexpected error", workerType);
            }

            if (UseDueScheduleGate)
                await WaitForNextRunAsync(stoppingToken);
        }
    }

    protected abstract Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct);

    protected virtual DateTime? CalculateNextFire(WorkerSchedule schedule) =>
        schedule.NextFireUtc?.Add(checkInterval);

    /// <summary>
    /// Called at the end of each loop iteration to sleep until the next run.
    /// Default behaviour is a fixed <c>checkInterval</c> delay.
    /// Override to implement event-driven wakeup (e.g. waiting on a schedule signal).
    /// </summary>
    protected virtual Task WaitForNextRunAsync(CancellationToken ct) =>
        Task.Delay(checkInterval, ct);
}
