using CrewService.Application.BackgroundWorkers;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Infrastructure;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class WorkerScheduleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkerSchedule>(dbContext, currentUserService), IWorkerScheduleRepository
{
    public async Task<IReadOnlyList<WorkerSchedule>> GetDueByTypeAsync(string workerType, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await DbContext.Set<WorkerSchedule>()
            .Where(s => s.WorkerType == workerType
                     && s.IsEnabled
                     && (s.NextFireUtc == null || s.NextFireUtc <= now))
            .ToListAsync(ct);
    }
}

internal sealed class WorkerExecutionLogRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<WorkerExecutionLog>(dbContext, currentUserService), IWorkerExecutionLogRepository
{
}
