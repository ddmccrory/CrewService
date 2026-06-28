using CrewService.Application.BackgroundWorkers;
using CrewService.Domain.Modules.Infrastructure;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Services;

internal sealed class ProcessingLockService(CrewServiceDbContext dbContext) : IProcessingLockService
{
    public async Task<ProcessingLock?> TryAcquireAsync(
        string lockKey, string instanceId, int expiryMinutes = 30, CancellationToken ct = default)
    {
        // Remove any expired lock for this key before trying to acquire
        var existing = await dbContext.ProcessingLocks
            .FirstOrDefaultAsync(l => l.LockKey == lockKey, ct);

        if (existing is not null)
        {
            if (!existing.IsExpired())
                return null; // Held by another instance

            dbContext.ProcessingLocks.Remove(existing);
        }

        var newLock = ProcessingLock.Acquire(lockKey, instanceId, expiryMinutes);
        dbContext.ProcessingLocks.Add(newLock);

        try
        {
            await dbContext.SaveChangesAsync(ct);
            return newLock;
        }
        catch (DbUpdateException)
        {
            // Another instance inserted concurrently
            return null;
        }
    }

    public async Task ReleaseAsync(string lockKey, CancellationToken ct = default)
    {
        var existing = await dbContext.ProcessingLocks
            .FirstOrDefaultAsync(l => l.LockKey == lockKey, ct);

        if (existing is null) return;

        dbContext.ProcessingLocks.Remove(existing);
        await dbContext.SaveChangesAsync(ct);
    }
}
