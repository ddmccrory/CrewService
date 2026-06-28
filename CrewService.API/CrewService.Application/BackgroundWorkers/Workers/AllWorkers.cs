using CrewService.Domain.Modules.Infrastructure;
using CrewService.Domain.Modules.Employees;
using CrewService.Application.Bulletins;
using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Application.Qualifications;
using CrewService.Application.SeniorityOps;
using CrewService.Application.Policies;
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
    ILogger<BulletinProcessingWorker> logger,
    IBulletinScheduleSignal scheduleSignal)
    : WorkerBase(scopeFactory, logger, "Bulletin", TimeSpan.FromMinutes(5))
{
    /// <summary>
    /// Before the poll loop starts, seed the signal with the earliest known bulletin event
    /// so the worker wakes at exactly the right time on the first iteration.
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var bulletinsService = scope.ServiceProvider.GetRequiredService<BulletinsService>();
        var nextEvent = await bulletinsService.GetNextBulletinEventUtcAsync(cancellationToken);

        if (nextEvent.HasValue)
        {
            scheduleSignal.Notify(nextEvent.Value);
            logger.LogInformation(
                "BulletinProcessingWorker: Startup — next bulletin event at {NextEvent:u}.", nextEvent.Value);
        }
        else
        {
            logger.LogInformation("BulletinProcessingWorker: Startup — no pending bulletin events.");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var bulletinsService = services.GetRequiredService<BulletinsService>();

        // 1. Auto-award all closed bulletins whose bid window has passed
        //    (or transition to NoBid if no qualified bidder exists — Notify is called inside)
        var awarded = await bulletinsService.AutoAwardClosedBulletinsAsync(ct);
        if (awarded.Count > 0)
            logger.LogInformation("BulletinProcessingWorker: Auto-awarded {Count} bulletin(s).", awarded.Count);

        // 2. Auto-force-assign all NoBid bulletins that have passed their force-assign deadline
        var forceAssigned = await bulletinsService.AutoForceAssignNoBidsAsync(ct);
        if (forceAssigned.Count > 0)
            logger.LogInformation("BulletinProcessingWorker: Auto-force-assigned {Count} NoBid bulletin(s).", forceAssigned.Count);

        // 3. Durable reconciliation: repost any vacant crew/board positions the post-commit
        //    reactor missed (e.g. process restart before the fire-and-forget reaction ran).
        var vacancyRepostService = services.GetRequiredService<VacancyAssignment.VacancyRepostService>();
        await vacancyRepostService.ReconcileUnbulletinedVacantPositionsAsync(ct);
    }

    /// <summary>
    /// Instead of sleeping a fixed interval, wait on the schedule signal.
    /// The signal wakes exactly when the next bulletin event time arrives, or sooner
    /// if a new bulletin is posted with an earlier close time.
    /// </summary>
    protected override Task WaitForNextRunAsync(CancellationToken ct) =>
        scheduleSignal.WaitAsync(ct);
}

public sealed class SeniorityMoveWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SeniorityMoveWorker> logger,
    ISeniorityMoveSignal scheduleSignal)
    : WorkerBase(scopeFactory, logger, "SeniorityMove", TimeSpan.FromMinutes(10))
{
    protected override async Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var policiesService = services.GetRequiredService<Policies.PoliciesService>();
        var executionService = services.GetRequiredService<Policies.SeniorityMoveExecutionService>();

        // Auto-approve any pending moves whose craft policy allows it
        var approved = await policiesService.AutoApprovePendingMovesAsync(ct);
        if (approved.Count > 0)
            logger.LogInformation("SeniorityMoveWorker: Auto-approved {Count} pending seniority move(s).", approved.Count);

        // Execute all approved moves whose effective time has arrived
        var due = await policiesService.GetApprovedDueSeniorityMovesAsync(DateTime.UtcNow, ct);
        foreach (var move in due)
        {
            try
            {
                await executionService.ExecuteAsync(move.CtrlNbr, ct);
                logger.LogInformation("SeniorityMoveWorker: Executed move {MoveCtrlNbr}.", move.CtrlNbr);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SeniorityMoveWorker: Failed to execute move {MoveCtrlNbr}.", move.CtrlNbr);
            }
        }

        // Set the next wakeup to the earliest remaining approved move
        var nextDue = await policiesService.GetNextApprovedSeniorityMoveEffectiveUtcAsync(ct);
        if (nextDue.HasValue)
            scheduleSignal.Notify(nextDue.Value);
    }

    protected override Task WaitForNextRunAsync(CancellationToken ct) =>
        scheduleSignal.WaitAsync(ct);
}

public sealed class SeniorityStateChangeWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SeniorityStateChangeWorker> logger,
    ISeniorityStateChangeSignal scheduleSignal)
    : WorkerBase(scopeFactory, logger, "SeniorityStateChange", TimeSpan.FromMinutes(5))
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var seniorityService = scope.ServiceProvider.GetRequiredService<SeniorityAppService>();
        var nextEvent = await seniorityService.GetNextPendingChangeUtcAsync(cancellationToken);

        if (nextEvent.HasValue)
        {
            scheduleSignal.Notify(nextEvent.Value);
            logger.LogInformation(
                "SeniorityStateChangeWorker: Startup — next pending state change at {NextEvent:u}.", nextEvent.Value);
        }
        else
        {
            logger.LogInformation("SeniorityStateChangeWorker: Startup — no pending state changes.");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var seniorityService = services.GetRequiredService<SeniorityAppService>();

        var applied = await seniorityService.ApplyDuePendingChangesAsync(ct);
        if (applied > 0)
            logger.LogInformation("SeniorityStateChangeWorker: Applied {Count} pending state change(s).", applied);

        // Re-seed the signal with the next remaining pending change, if any.
        var nextEvent = await seniorityService.GetNextPendingChangeUtcAsync(ct);
        if (nextEvent.HasValue)
            scheduleSignal.Notify(nextEvent.Value);
    }

    protected override Task WaitForNextRunAsync(CancellationToken ct) =>
        scheduleSignal.WaitAsync(ct);
}

public sealed class FraComplianceWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<FraComplianceWorker> logger)
    : WorkerBase(scopeFactory, logger, "FraCheck", TimeSpan.FromMinutes(5))
{
    protected override async Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var certifications = services.GetRequiredService<IEmployeeCertificationRepository>();
        var checkConfigRepo = services.GetRequiredService<IFraCertificationCheckConfigRepository>();
        var certConfigRepo = services.GetRequiredService<IFraCertificationConfigRepository>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var allWithChecks = await certifications.GetAllWithChecksAsync(ct);

        // Load all parent-level configs once. Employee certs don't carry a parent FK,
        // so use the single parent config if there is exactly one; otherwise fall back to defaults.
        var allCertConfigs = await certConfigRepo.GetAllAsync(ct);
        var allCheckConfigs = await checkConfigRepo.GetAllAsync(ct);
        var singleCertConfig = allCertConfigs.Count == 1 ? allCertConfigs[0] : null;
        IReadOnlyList<FraCertificationCheckConfig>? singleCheckConfigs =
            allCertConfigs.Count == 1 && allCheckConfigs.Count > 0
                ? allCheckConfigs.Where(c => c.ParentCtrlNbr == allCertConfigs[0].ParentCtrlNbr
                                          && c.RailroadCtrlNbr == null).ToList()
                : null;

        foreach (var cert in allWithChecks)
        {
            var before = cert.Status;
            cert.RecomputeStatus(today, singleCheckConfigs);

            if (cert.Status != before)
                await certifications.UpdateAsync(cert, ct);

            if (cert.Status is CertificationStatuses.Active or CertificationStatuses.Renew)
            {
                var recertWindowDays = singleCertConfig?.RecertWindowDays ?? 180;
                if (today >= cert.ExpirationDate.AddDays(-recertWindowDays))
                {
                    var existingCerts = await certifications.GetByEmployeeCtrlNbrAsync(cert.EmployeeCtrlNbr, ct);
                    var hasPendingRecert = existingCerts.Any(c =>
                        c.CtrlNbr != cert.CtrlNbr
                        && c.RegulatoryQualificationCtrlNbr == cert.RegulatoryQualificationCtrlNbr
                        && string.Equals(c.CertificationType, cert.CertificationType, StringComparison.OrdinalIgnoreCase)
                        && c.Status == CertificationStatuses.Pending
                        && c.CertificationDate > cert.CertificationDate);

                    if (!hasPendingRecert)
                    {
                        var certCycleMonths = singleCertConfig?.CertCycleMonths ?? 36;
                        var newCert = EmployeeCertification.Create(
                            employeeCtrlNbr: cert.EmployeeCtrlNbr,
                            regulatoryQualificationCtrlNbr: cert.RegulatoryQualificationCtrlNbr,
                            certificationType: cert.CertificationType,
                            certificationDate: cert.ExpirationDate,
                            recertificationIntervalMonths: certCycleMonths);
                        certifications.Add(newCert);
                        logger.LogInformation("Auto-initiated recertification for employee {EmployeeCtrlNbr}, expiring {ExpirationDate}",
                            cert.EmployeeCtrlNbr.Value, cert.ExpirationDate);
                    }
                }
            }
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

        var nowUtc = DateTime.UtcNow;
        var notifyWindowUtc = nowUtc.AddDays(EmployeeQualification.ExpiringSoonDays);
        var qualifications = await employeeQualificationRepository.GetExpiringBeforeAsync(notifyWindowUtc);
        var thresholds = new HashSet<int> { 60, 30, 14, 7 };

        foreach (var qualification in qualifications)
        {
            if (qualification.ExpiresAtUtc is null) continue;
            var daysRemaining = (int)Math.Floor((qualification.ExpiresAtUtc.Value - nowUtc).TotalDays);
            if (!thresholds.Contains(daysRemaining)) continue;
            // TODO: send notification via IOperationalNotifier when notification system is built out
        }
    }
}

public sealed class RequirementEvaluationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RequirementEvaluationWorker> logger)
    : WorkerBase(scopeFactory, logger, "PrereqEval", TimeSpan.FromHours(24))
{
    protected override async Task ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var employeeRepository = services.GetRequiredService<IEmployeeRepository>();
        var qualificationTypeRepository = services.GetRequiredService<IQualificationTypeRepository>();
        var requirementEvaluationService = services.GetRequiredService<RequirementEvaluationService>();

        var employees = await employeeRepository.GetAllAsync(ct);
        var qualificationTypes = await qualificationTypeRepository.GetAllAsync(ct);

        var strategySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            EvaluationStrategies.TimeFromEvent,
            EvaluationStrategies.ActivityCount,
            EvaluationStrategies.TimeInRole,
            EvaluationStrategies.QualificationHeld
        };

        foreach (var qualificationType in qualificationTypes.Where(q => q.IsActive && strategySet.Contains(q.EvaluationStrategy)))
        {
            foreach (var employee in employees)
            {
                await requirementEvaluationService.EvaluateAsync(employee.CtrlNbr, qualificationType, ct);
            }
        }
    }
}
