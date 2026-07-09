using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Domain = CrewService.Domain;
using Time = CrewService.Application.Time;

namespace CrewService.Presentation.Services.Modules;

public class BackgroundServicesService(IServiceProvider serviceProvider)
    : BackgroundServicesSrvc.BackgroundServicesSrvcBase
{
    private static readonly HashSet<string> EventDrivenWorkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "CallSheet",
        "Bulletin",
        "SeniorityMove",
        "SeniorityStateChange"
    };

    public override async Task<GetWorkerSchedulesResponse> GetWorkerSchedules(
        GetWorkerSchedulesRequest request, ServerCallContext context)
    {
        var scheduleRepo = serviceProvider.GetRequiredService<Application.BackgroundWorkers.IWorkerScheduleRepository>();
        var workAreaRepo = serviceProvider.GetRequiredService<Domain.Modules.TenantConfig.IDynamicGroupRepository>();
        var workAreaClock = serviceProvider.GetRequiredService<Time.IWorkAreaClock>();
        var callSheetScheduler = serviceProvider.GetRequiredService<Application.DailyOperations.IDailyCallSheetSchedulerService>();
        var bulletinsService = serviceProvider.GetRequiredService<Application.Bulletins.BulletinsService>();
        var policiesService = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var seniorityService = serviceProvider.GetRequiredService<Application.SeniorityOps.SeniorityAppService>();
        var uowFactory = serviceProvider.GetRequiredService<Domain.Interfaces.IOrchestrationUnitOfWorkFactory>();
        var heartbeatRegistry = serviceProvider.GetRequiredService<Application.BackgroundWorkers.IWorkerHeartbeatRegistry>();

        var schedules = await scheduleRepo.GetAllAsync(request.WorkerType, context.CancellationToken);

        var allWorkAreas = await workAreaRepo.GetWorkAreasAsync(
            request.HasRailroadCtrlNbr
                ? Domain.ValueObjects.ControlNumber.Create(request.RailroadCtrlNbr)
                : null);

        var workAreaByCtrlNbr = allWorkAreas.ToDictionary(w => w.CtrlNbr.Value);

        var scopedSchedules = schedules
            .Where(s => workAreaByCtrlNbr.ContainsKey(s.WorkAreaGroupCtrlNbr.Value))
            .GroupBy(s => new
            {
                WorkArea = s.WorkAreaGroupCtrlNbr.Value,
                WorkerTypeKey = s.WorkerType.ToUpperInvariant()
            })
            .Select(g => g.OrderByDescending(x => x.CtrlNbr.Value).First())
            .OrderBy(s => workAreaByCtrlNbr[s.WorkAreaGroupCtrlNbr.Value].Name)
            .ThenBy(s => s.WorkerType)
            .ToList();

        var response = new GetWorkerSchedulesResponse();
        foreach (var s in scopedSchedules)
        {
            var workArea = workAreaByCtrlNbr[s.WorkAreaGroupCtrlNbr.Value];
            var tz = workAreaClock.ResolveTimeZone(workArea.TimeZoneId);
            var suppressLastRun = string.Equals(s.LastRunStatus, "NoWork", StringComparison.OrdinalIgnoreCase);
            var effectiveNextFireUtc = await ResolveDisplayNextRunUtcAsync(
                s,
                workArea,
                callSheetScheduler,
                bulletinsService,
                policiesService,
                seniorityService,
                uowFactory,
                context.CancellationToken);
            var lastHeartbeatUtc = heartbeatRegistry.GetLastHeartbeatUtc(s.CtrlNbr);
            response.Schedules.Add(new WorkerScheduleResponse
            {
                CtrlNbr = s.CtrlNbr.Value,
                WorkerType = s.WorkerType,
                IsEnabled = s.IsEnabled,
                NextFireUtc = effectiveNextFireUtc.HasValue
                    ? Timestamp.FromDateTime(DateTime.SpecifyKind(effectiveNextFireUtc.Value, DateTimeKind.Utc))
                    : null,
                LastRunUtc = !suppressLastRun && s.LastRunUtc.HasValue
                    ? Timestamp.FromDateTime(DateTime.SpecifyKind(s.LastRunUtc.Value, DateTimeKind.Utc))
                    : null,
                LastRunStatus = suppressLastRun
                    ? string.Empty
                    : s.LastRunStatus ?? string.Empty,
                WorkAreaCtrlNbr = workArea.CtrlNbr.Value,
                WorkAreaName = (workArea.Code ?? string.Empty).ToUpperInvariant(),
                CronExpression = string.IsNullOrWhiteSpace(s.CronExpression) ? string.Empty : s.CronExpression,
                NextFireLocalDisplay = effectiveNextFireUtc.HasValue
                    ? workAreaClock.FormatLocalIso(effectiveNextFireUtc.Value, tz)
                    : string.Empty,
                LastRunLocalDisplay = !suppressLastRun && s.LastRunUtc.HasValue
                    ? workAreaClock.FormatLocalIso(s.LastRunUtc.Value, tz)
                    : string.Empty,
                LastWorkerHeartbeatUtc = lastHeartbeatUtc.HasValue
                    ? Timestamp.FromDateTime(DateTime.SpecifyKind(lastHeartbeatUtc.Value, DateTimeKind.Utc))
                    : null,
                LastWorkerHeartbeatLocalDisplay = lastHeartbeatUtc.HasValue
                    ? workAreaClock.FormatLocalIso(lastHeartbeatUtc.Value, tz)
                    : string.Empty,
            });
        }

        return response;
    }

    public override async Task<WorkerScheduleResponse> UpdateSchedule(
        UpdateScheduleRequest request, ServerCallContext context)
    {
        var repo = serviceProvider.GetRequiredService<Application.BackgroundWorkers.IWorkerScheduleRepository>();
        var schedule = await repo.GetByCtrlNbrAsync(Domain.ValueObjects.ControlNumber.Create(request.CtrlNbr), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Worker schedule {request.CtrlNbr} not found."));

        schedule.UpdateSchedule(request.IsEnabled, request.CronExpression);
        await repo.UpdateAsync(schedule, context.CancellationToken);

        return new WorkerScheduleResponse
        {
            CtrlNbr = schedule.CtrlNbr.Value,
            WorkerType = schedule.WorkerType,
            IsEnabled = schedule.IsEnabled,
            NextFireUtc = schedule.NextFireUtc.HasValue
                ? Timestamp.FromDateTime(DateTime.SpecifyKind(schedule.NextFireUtc.Value, DateTimeKind.Utc))
                : null,
            LastRunUtc = schedule.LastRunUtc.HasValue
                ? Timestamp.FromDateTime(DateTime.SpecifyKind(schedule.LastRunUtc.Value, DateTimeKind.Utc))
                : null,
            LastRunStatus = schedule.LastRunStatus ?? string.Empty,
            CronExpression = string.IsNullOrWhiteSpace(schedule.CronExpression) ? string.Empty : schedule.CronExpression,
            NextFireLocalDisplay = string.Empty,
            LastRunLocalDisplay = string.Empty,
        };
    }

    private static async Task<DateTime?> ResolveDisplayNextRunUtcAsync(
        Domain.Modules.Infrastructure.WorkerSchedule schedule,
        Domain.Modules.TenantConfig.DynamicGroup workArea,
        Application.DailyOperations.IDailyCallSheetSchedulerService callSheetScheduler,
        Application.Bulletins.BulletinsService bulletinsService,
        Application.Policies.PoliciesService policiesService,
        Application.SeniorityOps.SeniorityAppService seniorityService,
        Domain.Interfaces.IOrchestrationUnitOfWorkFactory uowFactory,
        CancellationToken ct)
    {
        if (!EventDrivenWorkers.Contains(schedule.WorkerType))
            return null;

        if (schedule.WorkerType.Equals("CallSheet", StringComparison.OrdinalIgnoreCase))
            return await callSheetScheduler.GetNextCallSheetEventUtcAsync(schedule.WorkAreaGroupCtrlNbr, ct);

        if (schedule.WorkerType.Equals("Bulletin", StringComparison.OrdinalIgnoreCase))
        {
            var (nextUtc, workAreaCtrlNbr) = await bulletinsService.GetNextBulletinEventAsync(workArea.OwningRailroadCtrlNbr, ct);
            return workAreaCtrlNbr == workArea.CtrlNbr.Value ? nextUtc : null;
        }

        if (schedule.WorkerType.Equals("SeniorityMove", StringComparison.OrdinalIgnoreCase))
        {
            var nextUtc = await policiesService.GetNextApprovedSeniorityMoveEffectiveUtcAsync(ct);
            if (!nextUtc.HasValue)
                return null;

            await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
            var dueMoves = await uow.SeniorityMoves.GetApprovedDueAsync(nextUtc.Value, ct);
            var matchingMove = dueMoves.FirstOrDefault(m => m.EffectiveUtc == nextUtc && m.RailroadCtrlNbr == workArea.OwningRailroadCtrlNbr);
            return matchingMove is not null ? nextUtc : null;
        }

        if (schedule.WorkerType.Equals("SeniorityStateChange", StringComparison.OrdinalIgnoreCase))
        {
            var (nextUtc, tzId) = await seniorityService.GetNextPendingChangeForRailroadAsync(workArea.OwningRailroadCtrlNbr, ct);
            if (!nextUtc.HasValue)
                return null;

            var workAreaTzId = workArea.TimeZoneId ?? string.Empty;
            var nextTzId = tzId ?? string.Empty;
            return string.Equals(workAreaTzId, nextTzId, StringComparison.OrdinalIgnoreCase) ? nextUtc : null;
        }

        return null;
    }

    public override async Task<GetExecutionLogsResponse> GetExecutionLogs(
        GetExecutionLogsRequest request, ServerCallContext context)
    {
        var repo = serviceProvider.GetRequiredService<Application.BackgroundWorkers.IWorkerExecutionLogRepository>();
        var logs = await repo.GetByScheduleAsync(
            Domain.ValueObjects.ControlNumber.Create(request.WorkerScheduleCtrlNbr),
            request.Limit,
            context.CancellationToken);

        var response = new GetExecutionLogsResponse();
        foreach (var l in logs)
        {
            response.Logs.Add(new ExecutionLogResponse
            {
                CtrlNbr = l.CtrlNbr.Value,
                Status = l.Status,
                StartedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(l.StartedAtUtc, DateTimeKind.Utc)),
                CompletedAt = l.CompletedAtUtc.HasValue
                    ? Timestamp.FromDateTime(DateTime.SpecifyKind(l.CompletedAtUtc.Value, DateTimeKind.Utc))
                    : null,
                ErrorMessage = l.ErrorMessage ?? string.Empty,
            });
        }

        return response;
    }
}
