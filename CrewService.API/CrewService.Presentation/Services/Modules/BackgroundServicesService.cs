using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using CrewService.Application.TenantConfig;
using Domain = CrewService.Domain;
using Time = CrewService.Application.Time;

namespace CrewService.Presentation.Services.Modules;

public class BackgroundServicesService(IServiceProvider serviceProvider)
    : BackgroundServicesSrvc.BackgroundServicesSrvcBase
{
    private static readonly SemaphoreSlim DefaultScheduleSeedGate = new(1, 1);

    private static readonly string[] SupportedWorkerTypes =
    [
        "AutoMarkUp",
        "Bulletin",
        "CallSheet",
        "CrewCalling",
        "DailyReport",
        "FraCheck",
        "MarkOff",
        "PayrollImport",
        "PrereqEval",
        "QualExpiryNotify",
        "RailroadInfoPublish",
        "SeniorityMove",
        "SeniorityStateChange",
        "Vacancy"
    ];

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
        var currentUserService = serviceProvider.GetRequiredService<Domain.Interfaces.ICurrentUserService>();
        var workAreaRepo = serviceProvider.GetRequiredService<Domain.Modules.TenantConfig.IDynamicGroupRepository>();
        var workAreaClock = serviceProvider.GetRequiredService<Time.IWorkAreaClock>();
        var nextRunResolver = serviceProvider.GetRequiredService<Application.BackgroundWorkers.IBackgroundJobNextRunResolver>();
        var heartbeatRegistry = serviceProvider.GetRequiredService<Application.BackgroundWorkers.IWorkerHeartbeatRegistry>();

        var schedules = await scheduleRepo.GetAllAsync(request.WorkerType, context.CancellationToken);

        if (string.IsNullOrWhiteSpace(currentUserService.GetUserName()))
            currentUserService.SetAuditOverride("BackgroundServicesService");

        var allWorkAreas = await workAreaRepo.GetWorkAreasAsync(
            request.HasRailroadCtrlNbr
                ? Domain.ValueObjects.ControlNumber.Create(request.RailroadCtrlNbr)
                : null);

        await DefaultScheduleSeedGate.WaitAsync(context.CancellationToken);
        try
        {
            schedules = await scheduleRepo.GetAllAsync(request.WorkerType, context.CancellationToken);

            await EnsureDefaultSchedulesAsync(
                scheduleRepo,
                allWorkAreas,
                schedules,
                request.WorkerType,
                context.CancellationToken);
        }
        finally
        {
            DefaultScheduleSeedGate.Release();
        }

        schedules = await scheduleRepo.GetAllAsync(request.WorkerType, context.CancellationToken);

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
            var effectiveNextFireUtc = await ResolveDisplayNextRunUtcAsync(
                s,
                workArea,
                nextRunResolver,
                serviceProvider.GetRequiredService<IRailroadResolver>(),
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
                LastRunUtc = s.LastRunUtc.HasValue
                    ? Timestamp.FromDateTime(DateTime.SpecifyKind(s.LastRunUtc.Value, DateTimeKind.Utc))
                    : null,
                LastRunStatus = s.LastRunStatus ?? string.Empty,
                WorkAreaCtrlNbr = workArea.CtrlNbr.Value,
                WorkAreaName = (workArea.Code ?? string.Empty).ToUpperInvariant(),
                CronExpression = string.IsNullOrWhiteSpace(s.CronExpression) ? string.Empty : s.CronExpression,
                NextFireLocalDisplay = effectiveNextFireUtc.HasValue
                    ? workAreaClock.FormatLocalIso(effectiveNextFireUtc.Value, tz)
                    : string.Empty,
                LastRunLocalDisplay = s.LastRunUtc.HasValue
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

    private static async Task EnsureDefaultSchedulesAsync(
        Application.BackgroundWorkers.IWorkerScheduleRepository scheduleRepo,
        IReadOnlyList<Domain.Modules.TenantConfig.DynamicGroup> workAreas,
        IReadOnlyList<Domain.Modules.Infrastructure.WorkerSchedule> existingSchedules,
        string? requestedWorkerType,
        CancellationToken ct)
    {
        if (workAreas.Count == 0)
            return;

        var targetWorkerTypes = string.IsNullOrWhiteSpace(requestedWorkerType)
            ? SupportedWorkerTypes
            : [requestedWorkerType];

        var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var schedule in existingSchedules)
            existingKeys.Add($"{schedule.WorkAreaGroupCtrlNbr.Value}:{schedule.WorkerType}");

        foreach (var workArea in workAreas)
        {
            foreach (var workerType in targetWorkerTypes)
            {
                var key = $"{workArea.CtrlNbr.Value}:{workerType}";
                if (!existingKeys.Add(key))
                    continue;

                await scheduleRepo.AddAsync(
                    Domain.Modules.Infrastructure.WorkerSchedule.Create(workArea.CtrlNbr, workerType),
                    ct);
            }
        }
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
        Application.BackgroundWorkers.IBackgroundJobNextRunResolver nextRunResolver,
        IRailroadResolver railroadResolver,
        CancellationToken ct)
    {
        if (!EventDrivenWorkers.Contains(schedule.WorkerType))
            return null;

        var railroadCtrlNbr = railroadResolver.ResolveFromGroup(workArea);
        if (railroadCtrlNbr is null)
            return null;

        var nextRun = await nextRunResolver.ResolveAsync(
            schedule.WorkerType,
            schedule.WorkAreaGroupCtrlNbr,
            railroadCtrlNbr,
            ct);

        return nextRun?.NextUtc;
    }

    public override async Task<GetExecutionLogsResponse> GetExecutionLogs(
        GetExecutionLogsRequest request, ServerCallContext context)
    {
        var repo = serviceProvider.GetRequiredService<Application.BackgroundWorkers.IWorkerExecutionLogRepository>();
        var scheduleRepo = serviceProvider.GetRequiredService<Application.BackgroundWorkers.IWorkerScheduleRepository>();
        var workAreaRepo = serviceProvider.GetRequiredService<Domain.Modules.TenantConfig.IDynamicGroupRepository>();
        var workAreaClock = serviceProvider.GetRequiredService<Time.IWorkAreaClock>();

        var schedule = await scheduleRepo.GetByCtrlNbrAsync(
            Domain.ValueObjects.ControlNumber.Create(request.WorkerScheduleCtrlNbr),
            context.CancellationToken);

        TimeZoneInfo? tz = null;
        if (schedule is not null)
        {
            var workArea = await workAreaRepo.GetByCtrlNbrAsync(schedule.WorkAreaGroupCtrlNbr, context.CancellationToken);
            tz = workAreaClock.ResolveTimeZone(workArea?.TimeZoneId);
        }

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
                StartedLocalDisplay = workAreaClock.FormatLocalIso(l.StartedAtUtc, tz),
                CompletedLocalDisplay = l.CompletedAtUtc.HasValue
                    ? workAreaClock.FormatLocalIso(l.CompletedAtUtc.Value, tz)
                    : string.Empty,
            });
        }

        return response;
    }
}
