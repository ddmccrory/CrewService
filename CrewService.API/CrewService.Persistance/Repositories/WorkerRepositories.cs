using CrewService.Application.BackgroundWorkers;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Infrastructure;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class WorkerScheduleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkerSchedule>(dbContext, currentUserService), IWorkerScheduleRepository
{
    private static IReadOnlyList<WorkerSchedule> CollapseDuplicateSchedules(IEnumerable<WorkerSchedule> schedules)
    {
        return schedules
            .GroupBy(s => new
            {
                WorkArea = s.WorkAreaGroupCtrlNbr,
                WorkerType = s.WorkerType.ToUpperInvariant()
            })
            .Select(g => g.OrderByDescending(s => s.CtrlNbr.Value).First())
            .ToList();
    }

    public async Task<IReadOnlyList<WorkerSchedule>> GetDueByTypeAsync(string workerType, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var due = await DbContext.Set<WorkerSchedule>()
            .Where(s => s.WorkerType == workerType
                     && s.IsEnabled
                     && (s.NextFireUtc == null || s.NextFireUtc <= now))
            .ToListAsync(ct);

        return CollapseDuplicateSchedules(due);
    }

    public async Task<IReadOnlyList<WorkerSchedule>> GetEnabledByTypeAsync(string workerType, CancellationToken ct = default)
    {
        var enabled = await DbContext.Set<WorkerSchedule>()
            .Where(s => s.WorkerType == workerType && s.IsEnabled)
            .ToListAsync(ct);

        return CollapseDuplicateSchedules(enabled);
    }

    public async Task<IReadOnlyList<WorkerSchedule>> GetAllAsync(string? workerType = null, CancellationToken ct = default)
    {
        var query = DbContext.Set<WorkerSchedule>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(workerType))
            query = query.Where(s => s.WorkerType == workerType);

        return await query.ToListAsync(ct);
    }
}

internal sealed class WorkerExecutionLogRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkerExecutionLog>(dbContext, currentUserService), IWorkerExecutionLogRepository
{
    public async Task<IReadOnlyList<WorkerExecutionLog>> GetByScheduleAsync(ControlNumber workerScheduleCtrlNbr, int limit = 20, CancellationToken ct = default)
    {
        return await DbContext.Set<WorkerExecutionLog>()
            .Where(l => l.WorkerScheduleCtrlNbr == workerScheduleCtrlNbr)
            .OrderByDescending(l => l.StartedAtUtc)
            .Take(limit)
            .ToListAsync(ct);
    }
}
