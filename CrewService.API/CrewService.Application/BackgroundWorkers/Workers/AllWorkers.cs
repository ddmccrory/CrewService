using CrewService.Domain.Modules.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.BackgroundWorkers.Workers;

public sealed class DailyCallSheetWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyCallSheetWorker> logger)
    : WorkerBase(scopeFactory, logger, "CallSheet", TimeSpan.FromMinutes(5))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        // Delegates to CallSheetGenerationService with work area + shift + target date
        return Task.CompletedTask;
    }
}

public sealed class VacancyAssignmentWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<VacancyAssignmentWorker> logger)
    : WorkerBase(scopeFactory, logger, "Vacancy", TimeSpan.FromMinutes(2))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        // Delegates to VacancyResolutionEngine — requires shift context per schedule
        return Task.CompletedTask;
    }
}

public sealed class MarkOffRequestWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<MarkOffRequestWorker> logger)
    : WorkerBase(scopeFactory, logger, "MarkOff", TimeSpan.FromMinutes(1))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

public sealed class AutoMarkUpWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AutoMarkUpWorker> logger)
    : WorkerBase(scopeFactory, logger, "AutoMarkUp", TimeSpan.FromMinutes(1))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        // Evaluates due AbsenceMarkUp records past scheduledMarkUpUtc
        return Task.CompletedTask;
    }
}

public sealed class BulletinProcessingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<BulletinProcessingWorker> logger)
    : WorkerBase(scopeFactory, logger, "Bulletin", TimeSpan.FromMinutes(5))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

public sealed class SeniorityMoveWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SeniorityMoveWorker> logger)
    : WorkerBase(scopeFactory, logger, "SeniorityMove", TimeSpan.FromMinutes(10))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

public sealed class FraComplianceWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<FraComplianceWorker> logger)
    : WorkerBase(scopeFactory, logger, "FraCheck", TimeSpan.FromMinutes(5))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

public sealed class CrewCallingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CrewCallingWorker> logger)
    : WorkerBase(scopeFactory, logger, "CrewCalling", TimeSpan.FromSeconds(30))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        // Polls pending NotificationRequests via CrewCallingService
        return Task.CompletedTask;
    }
}

public sealed class PayrollImportWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PayrollImportWorker> logger)
    : WorkerBase(scopeFactory, logger, "PayrollImport", TimeSpan.FromMinutes(5))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        // File polling + PayrollImportService
        return Task.CompletedTask;
    }
}

public sealed class DailyReportWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyReportWorker> logger)
    : WorkerBase(scopeFactory, logger, "DailyReport", TimeSpan.FromHours(1))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
