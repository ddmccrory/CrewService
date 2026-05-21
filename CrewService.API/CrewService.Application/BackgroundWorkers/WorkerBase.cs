using CrewService.Domain.Modules.Infrastructure;
using CrewService.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.BackgroundWorkers;

public interface IWorkerScheduleRepository
{
    Task<IReadOnlyList<WorkerSchedule>> GetDueByTypeAsync(string workerType, CancellationToken ct = default);
    Task UpdateAsync(WorkerSchedule schedule, CancellationToken ct = default);
}

public interface IWorkerExecutionLogRepository
{
    Task AddAsync(WorkerExecutionLog log, CancellationToken ct = default);
    Task UpdateAsync(WorkerExecutionLog log, CancellationToken ct = default);
}

public interface IProcessingLockService
{
    Task<ProcessingLock?> TryAcquireAsync(string lockKey, string instanceId, int expiryMinutes = 30, CancellationToken ct = default);
    Task ReleaseAsync(string lockKey, CancellationToken ct = default);
}

public abstract class WorkerBase(
    IServiceScopeFactory scopeFactory,
    ILogger logger,
    string workerType,
    TimeSpan checkInterval) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scheduleRepo = scope.ServiceProvider.GetRequiredService<IWorkerScheduleRepository>();
                var logRepo = scope.ServiceProvider.GetRequiredService<IWorkerExecutionLogRepository>();
                var lockService = scope.ServiceProvider.GetRequiredService<IProcessingLockService>();

                var dueSchedules = await scheduleRepo.GetDueByTypeAsync(workerType, stoppingToken);

                foreach (var schedule in dueSchedules)
                {
                    var lockKey = $"{workerType}:{schedule.WorkAreaGroupCtrlNbr.Value}";
                    var processingLock = await lockService.TryAcquireAsync(lockKey, Environment.MachineName, ct: stoppingToken);

                    if (processingLock is null)
                    {
                        if (logger.IsEnabled(LogLevel.Information))
                            logger.LogInformation("Lock {LockKey} held by another instance, skipping", lockKey);
                        continue;
                    }

                    var executionLog = WorkerExecutionLog.Start(schedule.CtrlNbr);
                    await logRepo.AddAsync(executionLog, stoppingToken);

                    try
                    {
                        await ExecuteWorkAsync(scope.ServiceProvider, schedule, stoppingToken);
                        executionLog.Complete();
                        schedule.RecordSuccess(CalculateNextFire(schedule));
                    }
                    catch (Exception ex)
                    {
                        executionLog.Fail(ex.Message);
                        schedule.RecordFailure(CalculateNextFire(schedule));
                        logger.LogError(ex, "Worker {WorkerType} failed for schedule {ScheduleCtrlNbr}", workerType, schedule.CtrlNbr.Value);
                    }
                    finally
                    {
                        await logRepo.UpdateAsync(executionLog, stoppingToken);
                        await scheduleRepo.UpdateAsync(schedule, stoppingToken);
                        await lockService.ReleaseAsync(lockKey, stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Worker {WorkerType} encountered an unexpected error", workerType);
            }

            await WaitForNextRunAsync(stoppingToken);
        }
    }

    protected abstract Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct);

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
