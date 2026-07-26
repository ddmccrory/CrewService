using CrewService.Domain.Modules.Infrastructure;
using CrewService.Domain.Modules.Employees;
using CrewService.Application.Bulletins;
using CrewService.Application.FraCompliance;
using CrewService.Domain.Modules.FraCompliance;
using CrewService.Application.Qualifications;
using CrewService.Application.SeniorityOps;
using CrewService.Application.Policies;
using CrewService.Application.TenantConfig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrewService.Application.BackgroundWorkers.Workers;

public sealed class DailyCallSheetWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyCallSheetWorker> logger,
    IDailyCallSheetScheduleSignal scheduleSignal,
    IDailyCallSheetManualOverrideStore manualOverrideStore)
    : WorkerBase(scopeFactory, logger, "CallSheet", TimeSpan.FromMinutes(5))
{
    protected override bool UseDueScheduleGate => false;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<DailyOperations.IDailyCallSheetSchedulerService>();
        var scheduleRepo = scope.ServiceProvider.GetRequiredService<IWorkerScheduleRepository>();
        var schedules = await scheduleRepo.GetEnabledByTypeAsync("CallSheet", cancellationToken);

        DateTime? earliest = null;
        foreach (var workerSchedule in schedules)
        {
            var next = await scheduler.GetNextCallSheetEventUtcAsync(workerSchedule.WorkAreaGroupCtrlNbr, cancellationToken);
            if (next.HasValue && (earliest is null || next.Value < earliest.Value))
                earliest = next;
        }

        if (earliest.HasValue)
        {
            scheduleSignal.Notify(earliest.Value);
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("DailyCallSheetWorker: Startup — next call-sheet event at {NextEvent:u}.", earliest.Value);
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("DailyCallSheetWorker: Startup — no pending call-sheet events.");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var scheduler = services.GetRequiredService<DailyOperations.IDailyCallSheetSchedulerService>();
        var generator = services.GetRequiredService<DailyOperations.CallSheetGenerationService>();
        var nextRunResolver = services.GetRequiredService<IBackgroundJobNextRunResolver>();
        var railroadResolver = services.GetRequiredService<IRailroadResolver>();
        var dynamicGroupRepo = services.GetRequiredService<Domain.Modules.TenantConfig.IDynamicGroupRepository>();

        var dueItems = await scheduler.GetDueWorkItemsAsync(schedule.WorkAreaGroupCtrlNbr, DateTime.UtcNow, ct);
        var manualDue = manualOverrideStore.DequeueDue(schedule.WorkAreaGroupCtrlNbr, DateTime.UtcNow);
        if (manualDue.Count > 0)
        {
            var merged = dueItems.ToList();
            foreach (var item in manualDue)
            {
                if (!merged.Any(x =>
                    x.WorkAreaGroupCtrlNbr == item.WorkAreaGroupCtrlNbr
                    && x.ShiftDefinitionCtrlNbr == item.ShiftDefinitionCtrlNbr
                    && x.TargetDate == item.TargetDate
                    && x.DepartmentCtrlNbr == item.DepartmentCtrlNbr))
                {
                    merged.Add(item);
                }
            }

            dueItems = merged;
        }

        if (dueItems.Count == 0)
            return false;

        var didWork = false;
        foreach (var item in dueItems)
        {
            try
            {
                await generator.GenerateForShiftAsync(
                    item.WorkAreaGroupCtrlNbr,
                    item.ShiftDefinitionCtrlNbr,
                    item.TargetDate,
                    item.DepartmentCtrlNbr,
                    ct);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "DailyCallSheetWorker: Generated call sheet for work area {WorkAreaCtrlNbr}, shift {ShiftDefinitionCtrlNbr}, date {TargetDate}.",
                        item.WorkAreaGroupCtrlNbr,
                        item.ShiftDefinitionCtrlNbr,
                        item.TargetDate);
                }
                didWork = true;
            }
            catch (InvalidOperationException ex)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        ex,
                        "DailyCallSheetWorker: Skipped call sheet generation for work area {WorkAreaCtrlNbr}, shift {ShiftDefinitionCtrlNbr}, date {TargetDate}.",
                        item.WorkAreaGroupCtrlNbr,
                        item.ShiftDefinitionCtrlNbr,
                        item.TargetDate);
                }
            }
        }

        var workArea = await dynamicGroupRepo.GetByCtrlNbrAsync(schedule.WorkAreaGroupCtrlNbr, ct);
        DateTime? nextEvent = null;
        if (workArea is not null)
        {
            var railroadCtrlNbr = railroadResolver.ResolveFromGroup(workArea);
            if (railroadCtrlNbr is null)
                return didWork;

            var nextRun = await nextRunResolver.ResolveAsync(
                "CallSheet",
                schedule.WorkAreaGroupCtrlNbr,
                railroadCtrlNbr,
                ct);
            nextEvent = nextRun?.NextUtc;
        }

        if (nextEvent.HasValue)
            scheduleSignal.Notify(nextEvent.Value);

        return didWork;
    }

    protected override Task WaitForNextRunAsync(CancellationToken ct) =>
        scheduleSignal.WaitAsync(ct);

    protected override DateTime? CalculateNextFire(WorkerSchedule schedule) => null;
}

public sealed class VacancyAssignmentWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<VacancyAssignmentWorker> logger)
    : WorkerBase(scopeFactory, logger, "Vacancy", TimeSpan.FromMinutes(2))
{
    protected override Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        // Delegates to VacancyResolutionEngine — requires shift context per schedule
        return Task.FromResult(false);
    }
}

public sealed class MarkOffRequestWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<MarkOffRequestWorker> logger,
    IAbsenceMarkOffSignal scheduleSignal)
    : WorkerBase(scopeFactory, logger, "MarkOff", TimeSpan.FromMinutes(1))
{
    protected override bool UseDueScheduleGate => false;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var absenceRequestService = scope.ServiceProvider.GetRequiredService<AbsenceVacancy.AbsenceRequestService>();
        var nextEvent = await absenceRequestService.GetNextApprovedAutoMarkOffStartUtcAsync(cancellationToken);

        if (nextEvent.HasValue)
        {
            scheduleSignal.Notify(nextEvent.Value);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "MarkOffRequestWorker: Startup — next auto mark-off event at {NextEvent:u}.", nextEvent.Value);
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("MarkOffRequestWorker: Startup — no pending auto mark-off events.");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var absenceRequestService = services.GetRequiredService<AbsenceVacancy.AbsenceRequestService>();
        var processed = await absenceRequestService.ExecuteDueAutoMarkOffAsync(DateTime.UtcNow, ct);

        if (processed > 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("MarkOffRequestWorker: Auto marked off {Count} approved request(s).", processed);
        }

        var nextEvent = await absenceRequestService.GetNextApprovedAutoMarkOffStartUtcAsync(ct);
        if (nextEvent.HasValue)
            scheduleSignal.Notify(nextEvent.Value);

        return processed > 0;
    }

    protected override Task WaitForNextRunAsync(CancellationToken ct) =>
        scheduleSignal.WaitAsync(ct);

    protected override DateTime? CalculateNextFire(WorkerSchedule schedule) => null;

}

public sealed class AutoMarkUpWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AutoMarkUpWorker> logger,
    IAutoMarkUpSignal scheduleSignal)
    : WorkerBase(scopeFactory, logger, "AutoMarkUp", TimeSpan.FromMinutes(1))
{
    protected override bool UseDueScheduleGate => false;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var absenceRequestService = scope.ServiceProvider.GetRequiredService<AbsenceVacancy.AbsenceRequestService>();
        var nextEvent = await absenceRequestService.GetNextScheduledEndUtcAsync(cancellationToken);

        if (nextEvent.HasValue)
        {
            scheduleSignal.Notify(nextEvent.Value);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "AutoMarkUpWorker: Startup — next scheduled end event at {NextEvent:u}.", nextEvent.Value);
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("AutoMarkUpWorker: Startup — no pending scheduled end events.");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var absenceRequestService = services.GetRequiredService<AbsenceVacancy.AbsenceRequestService>();
        var ended = await absenceRequestService.ExecuteDueScheduledEndAsync(DateTime.UtcNow, ct);

        if (ended > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("AutoMarkUpWorker: Auto ended {Count} open request(s) at scheduled end time.", ended);
        }

        var nextEvent = await absenceRequestService.GetNextScheduledEndUtcAsync(ct);
        if (nextEvent.HasValue)
            scheduleSignal.Notify(nextEvent.Value);

        return ended > 0;
    }

    protected override Task WaitForNextRunAsync(CancellationToken ct) =>
        scheduleSignal.WaitAsync(ct);

    protected override DateTime? CalculateNextFire(WorkerSchedule schedule) => null;
}

public sealed class WaitListReassignmentWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<WaitListReassignmentWorker> logger,
    IWaitListReassignmentSignal scheduleSignal)
    : WorkerBase(scopeFactory, logger, "WaitListReassignment", TimeSpan.FromMinutes(5))
{
    protected override bool UseDueScheduleGate => false;

    protected override async Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var processor = services.GetRequiredService<AbsenceVacancy.AbsenceWaitListReassignmentProcessor>();
        var processed = await processor.ProcessAsync(DateTime.UtcNow, ct);

        if (processed > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "WaitListReassignmentWorker: Assigned {Count} waitlist request(s) for schedule {ScheduleCtrlNbr}.",
                processed,
                schedule.CtrlNbr.Value);
        }

        return processed > 0;
    }

    protected override Task WaitForNextRunAsync(CancellationToken ct) =>
        scheduleSignal.WaitAsync(ct);
}

public sealed class BulletinProcessingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<BulletinProcessingWorker> logger,
    IBulletinScheduleSignal scheduleSignal)
    : WorkerBase(scopeFactory, logger, "Bulletin", TimeSpan.FromMinutes(5))
{
    protected override bool UseDueScheduleGate => false;

    /// <summary>
    /// Before the poll loop starts, seed the signal with the earliest known bulletin event
    /// so the worker wakes at exactly the right time on the first iteration.
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var bulletinsService = scope.ServiceProvider.GetRequiredService<BulletinsService>();

        var scheduleRepo = scope.ServiceProvider.GetRequiredService<IWorkerScheduleRepository>();
        var dynamicGroupRepo = scope.ServiceProvider.GetRequiredService<Domain.Modules.TenantConfig.IDynamicGroupRepository>();
        var schedules = await scheduleRepo.GetEnabledByTypeAsync("Bulletin", cancellationToken);

        DateTime? earliest = null;
        foreach (var workerSchedule in schedules)
        {
            var workArea = await dynamicGroupRepo.GetByCtrlNbrAsync(workerSchedule.WorkAreaGroupCtrlNbr, cancellationToken);
            if (workArea is null)
                continue;

            var next = await bulletinsService.GetNextBulletinEventUtcAsync(cancellationToken);
            if (next.HasValue && (earliest is null || next.Value < earliest.Value))
                earliest = next;
        }

        if (earliest.HasValue)
        {
            scheduleSignal.Notify(earliest.Value);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "BulletinProcessingWorker: Startup — next bulletin event at {NextEvent:u}.", earliest.Value);
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("BulletinProcessingWorker: Startup — no pending bulletin events.");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var bulletinsService = services.GetRequiredService<BulletinsService>();
        var dynamicGroupRepo = services.GetRequiredService<Domain.Modules.TenantConfig.IDynamicGroupRepository>();
        var workArea = await dynamicGroupRepo.GetByCtrlNbrAsync(schedule.WorkAreaGroupCtrlNbr, ct);
        if (workArea is null)
        {
            logger.LogWarning("BulletinProcessingWorker: work area {WorkAreaCtrlNbr} not found; skipping.", schedule.WorkAreaGroupCtrlNbr.Value);
            return false;
        }

        // 1. Auto-award all closed bulletins whose bid window has passed
        //    (or transition to NoBid if no qualified bidder exists — Notify is called inside)
        var awarded = await bulletinsService.AutoAwardClosedBulletinsAsync(ct);
        if (awarded.Count > 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("BulletinProcessingWorker: Auto-awarded {Count} bulletin(s).", awarded.Count);
        }

        // 2. Auto-force-assign all NoBid bulletins that have passed their force-assign deadline
        var forceAssigned = await bulletinsService.AutoForceAssignNoBidsAsync(ct);
        if (forceAssigned.Count > 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("BulletinProcessingWorker: Auto-force-assigned {Count} NoBid bulletin(s).", forceAssigned.Count);
        }

        var didWork = awarded.Count > 0 || forceAssigned.Count > 0;

        // 3. Durable reconciliation: repost any vacant crew/board positions the inline repost
        //    missed (e.g. process restart mid-request before the synchronous repost ran).
        if (didWork)
        {
            var vacancyRepostService = services.GetRequiredService<VacancyAssignment.IVacancyRepostService>();
            await vacancyRepostService.ReconcileUnbulletinedVacantPositionsAsync(ct);
        }

        // Re-seed with the next pending event after processing so the signal always targets
        // the current earliest close/deadline, including tomorrow's event after processing today's.
        var nextEvent = await bulletinsService.GetNextBulletinEventUtcAsync(ct);
        if (nextEvent.HasValue)
            scheduleSignal.Notify(nextEvent.Value);

        return didWork;
    }

    protected override DateTime? CalculateNextFire(WorkerSchedule schedule) => null;

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
    protected override bool UseDueScheduleGate => false;

    protected override async Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var policiesService = services.GetRequiredService<Policies.PoliciesService>();
        var executionService = services.GetRequiredService<Policies.SeniorityMoveExecutionService>();
        var didWork = false;

        // Auto-approve any pending moves whose craft policy allows it
        var approved = await policiesService.AutoApprovePendingMovesAsync(ct);
        if (approved.Count > 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("SeniorityMoveWorker: Auto-approved {Count} pending seniority move(s).", approved.Count);
            didWork = true;
        }

        // Execute all approved moves whose effective time has arrived
        var due = await policiesService.GetApprovedDueSeniorityMovesAsync(DateTime.UtcNow, ct);
        if (due.Count == 0)
        {
            var nextDueOnly = await policiesService.GetNextApprovedSeniorityMoveEffectiveUtcAsync(ct);
            if (nextDueOnly.HasValue)
                scheduleSignal.Notify(nextDueOnly.Value);

            return didWork;
        }

        foreach (var move in due)
        {
            try
            {
                await executionService.ExecuteAsync(move.CtrlNbr, ct);
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("SeniorityMoveWorker: Executed move {MoveCtrlNbr}.", move.CtrlNbr);
                didWork = true;
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

        return didWork;
    }

    protected override Task WaitForNextRunAsync(CancellationToken ct) =>
        scheduleSignal.WaitAsync(ct);

    protected override DateTime? CalculateNextFire(WorkerSchedule schedule) => null;
}

public sealed class SeniorityStateChangeWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SeniorityStateChangeWorker> logger,
    ISeniorityStateChangeSignal scheduleSignal)
    : WorkerBase(scopeFactory, logger, "SeniorityStateChange", TimeSpan.FromMinutes(5))
{
    protected override bool UseDueScheduleGate => false;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var seniorityService = scope.ServiceProvider.GetRequiredService<SeniorityAppService>();
        var nextEvent = await seniorityService.GetNextPendingChangeUtcAsync(cancellationToken);

        if (nextEvent.HasValue)
        {
            scheduleSignal.Notify(nextEvent.Value);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "SeniorityStateChangeWorker: Startup — next pending state change at {NextEvent:u}.", nextEvent.Value);
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("SeniorityStateChangeWorker: Startup — no pending state changes.");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var seniorityService = services.GetRequiredService<SeniorityAppService>();

        var applied = await seniorityService.ApplyDuePendingChangesAsync(ct);
        if (applied > 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("SeniorityStateChangeWorker: Applied {Count} pending state change(s).", applied);
        }

        // Re-seed the signal with the next remaining pending change, if any.
        var nextEvent = await seniorityService.GetNextPendingChangeUtcAsync(ct);
        if (nextEvent.HasValue)
            scheduleSignal.Notify(nextEvent.Value);

        return applied > 0;
    }

    protected override Task WaitForNextRunAsync(CancellationToken ct) =>
        scheduleSignal.WaitAsync(ct);

    protected override DateTime? CalculateNextFire(WorkerSchedule schedule) => null;
}

public sealed class FraComplianceWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<FraComplianceWorker> logger)
    : WorkerBase(scopeFactory, logger, "FraCheck", TimeSpan.FromMinutes(5))
{
    protected override async Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        var certifications = services.GetRequiredService<IEmployeeCertificationRepository>();
        var checkConfigRepo = services.GetRequiredService<IFraCertificationCheckConfigRepository>();
        var certConfigRepo = services.GetRequiredService<IFraCertificationConfigRepository>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var didWork = false;

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
            {
                await certifications.UpdateAsync(cert, ct);
                didWork = true;
            }

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
                        if (logger.IsEnabled(LogLevel.Information))
                        {
                            logger.LogInformation("Auto-initiated recertification for employee {EmployeeCtrlNbr}, expiring {ExpirationDate}",
                                cert.EmployeeCtrlNbr.Value, cert.ExpirationDate);
                        }
                        didWork = true;
                    }
                }
            }
        }

        return didWork;
    }
}

public sealed class CrewCallingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CrewCallingWorker> logger)
    : WorkerBase(scopeFactory, logger, "CrewCalling", TimeSpan.FromSeconds(30))
{
    protected override Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        // Polls pending NotificationRequests via CrewCallingService
        return Task.FromResult(false);
    }
}

public sealed class PayrollImportWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PayrollImportWorker> logger)
    : WorkerBase(scopeFactory, logger, "PayrollImport", TimeSpan.FromMinutes(5))
{
    protected override Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        // File polling + PayrollImportService
        return Task.FromResult(false);
    }
}

public sealed class DailyReportWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyReportWorker> logger)
    : WorkerBase(scopeFactory, logger, "DailyReport", TimeSpan.FromHours(1))
{
    protected override Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        return Task.FromResult(false);
    }
}

public sealed class RailroadInfoPublishWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RailroadInfoPublishWorker> logger)
    : WorkerBase(scopeFactory, logger, "RailroadInfoPublish", TimeSpan.FromMinutes(5))
{
    protected override Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
    {
        // Publishes scheduled RailroadInformation records whose publish time has arrived
        return Task.FromResult(false);
    }
}

public sealed class QualificationExpiryNotifierWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<QualificationExpiryNotifierWorker> logger)
    : WorkerBase(scopeFactory, logger, "QualExpiryNotify", TimeSpan.FromHours(24))
{
    protected override async Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
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

        return false;
    }
}

public sealed class RequirementEvaluationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RequirementEvaluationWorker> logger)
    : WorkerBase(scopeFactory, logger, "PrereqEval", TimeSpan.FromHours(24))
{
    protected override async Task<bool> ExecuteWorkAsync(IServiceProvider services, WorkerSchedule schedule, CancellationToken ct)
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

        return false;
    }
}
