using CrewService.Domain.Modules.Infrastructure;
using CrewService.Domain.Modules.Employees;
using CrewService.Application.FraCompliance;
using CrewService.Application.Qualifications;
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
    protected override async Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var certifications = services.GetRequiredService<IEmployeeCertificationRepository>();

        var allCertifications = await certifications.GetAllAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var cert in allCertifications.Where(c => c.Status == "Active" && c.ExpirationDate <= today))
        {
            cert.Expire();
            await certifications.UpdateAsync(cert, ct);
        }
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

public sealed class RailroadInfoPublishWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RailroadInfoPublishWorker> logger)
    : WorkerBase(scopeFactory, logger, "RailroadInfoPublish", TimeSpan.FromMinutes(5))
{
    protected override Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        // Publishes scheduled RailroadInformation records whose publish time has arrived
        return Task.CompletedTask;
    }
}

public sealed class QualificationExpiryNotifierWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<QualificationExpiryNotifierWorker> logger)
    : WorkerBase(scopeFactory, logger, "QualExpiryNotify", TimeSpan.FromHours(24))
{
    protected override async Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var employeeQualificationRepository = services.GetRequiredService<IEmployeeQualificationRepository>();

        var qualifications = await employeeQualificationRepository.GetAllAsync(ct);
        var nowUtc = DateTime.UtcNow;
        var thresholds = new HashSet<int> { 60, 30, 14, 7 };

        foreach (var qualification in qualifications)
        {
            if (qualification.ExpiresAtUtc is null)
                continue;

            if (qualification.Status is not ("Active" or "ExpiringSoon"))
                continue;

            var daysRemaining = (int)Math.Floor((qualification.ExpiresAtUtc.Value - nowUtc).TotalDays);
            if (thresholds.Contains(daysRemaining) && qualification.Status == "Active")
            {
                qualification.MarkExpiringSoon(daysRemaining);
                await employeeQualificationRepository.UpdateAsync(qualification, ct);
            }
        }
    }
}

public sealed class QualificationExpiryEnforcerWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<QualificationExpiryEnforcerWorker> logger)
    : WorkerBase(scopeFactory, logger, "QualExpiryEnforce", TimeSpan.FromHours(1))
{
    protected override async Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var employeeQualificationRepository = services.GetRequiredService<IEmployeeQualificationRepository>();
        var qualificationTypeRepository = services.GetRequiredService<IQualificationTypeRepository>();

        var nowUtc = DateTime.UtcNow;
        var candidates = await employeeQualificationRepository.GetExpiringBeforeAsync(nowUtc);

        foreach (var qualification in candidates)
        {
            if (qualification.ExpiresAtUtc is null)
                continue;

            var qualificationType = await qualificationTypeRepository
                .GetByCtrlNbrAsync(qualification.QualificationTypeCtrlNbr, ct);

            var graceDays = qualificationType?.GraceDays ?? 0;
            var hardExpiryUtc = qualification.ExpiresAtUtc.Value.AddDays(graceDays);

            if (hardExpiryUtc >= nowUtc)
                continue;

            if (qualification.Status is "Expired" or "Revoked")
                continue;

            qualification.Expire();
            await employeeQualificationRepository.UpdateAsync(qualification, ct);
        }
    }
}

public sealed class PrerequisiteEvaluationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PrerequisiteEvaluationWorker> logger)
    : WorkerBase(scopeFactory, logger, "PrereqEval", TimeSpan.FromHours(24))
{
    protected override async Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var employeeRepository = services.GetRequiredService<IEmployeeRepository>();
        var qualificationTypeRepository = services.GetRequiredService<IQualificationTypeRepository>();
        var prerequisiteEvaluationService = services.GetRequiredService<PrerequisiteEvaluationService>();

        var employees = await employeeRepository.GetAllAsync(ct);
        var qualificationTypes = await qualificationTypeRepository.GetAllAsync(ct);

        var strategySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TimeFromEvent",
            "ActivityCount",
            "TimeInRole",
            "QualificationHeld"
        };

        foreach (var qualificationType in qualificationTypes.Where(q => q.IsActive && strategySet.Contains(q.EvaluationStrategy)))
        {
            foreach (var employee in employees)
            {
                await prerequisiteEvaluationService.EvaluateAsync(employee.CtrlNbr, qualificationType, ct);
            }
        }
    }
}
