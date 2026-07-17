using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EmployeeNotificationRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EmployeeNotification>(dbContext, currentUserService), IEmployeeNotificationRepository
{
    public override async Task<EmployeeNotification?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<EmployeeNotification>()
            .Include(n => n.Acknowledgements)
            .SingleOrDefaultAsync(n => n.CtrlNbr == ctrlNbr, ct);

    public async Task<List<EmployeeNotification>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<EmployeeNotification>()
            .Include(n => n.Acknowledgements)
            .Where(n => n.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<List<EmployeeNotification>> GetUnacknowledgedByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<EmployeeNotification>()
            .Include(n => n.Acknowledgements)
            .Where(n => n.EmployeeCtrlNbr == employeeCtrlNbr
                && n.RequiresAcknowledgement
                && !n.Acknowledgements.Any(a => a.Confirmed))
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<List<EmployeeNotification>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<EmployeeNotification>()
            .Include(n => n.Acknowledgements)
            .Where(n => n.RailroadCtrlNbr == railroadCtrlNbr)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<int> CountUnacknowledgedByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<EmployeeNotification>()
            .Where(n => n.RailroadCtrlNbr == railroadCtrlNbr
                && n.RequiresAcknowledgement
                && !n.Acknowledgements.Any(a => a.Confirmed))
            .CountAsync(ct);

    public async Task<List<PositionChangeRecord>> GetOpenPositionChangesByNotificationAsync(
        ControlNumber employeeNotificationCtrlNbr,
        CancellationToken ct = default) =>
        await DbContext.Set<PositionChangeRecord>()
            .Where(r => r.EmployeeNotificationCtrlNbr == employeeNotificationCtrlNbr && r.IsOpen)
            .OrderByDescending(r => r.OpenedAtUtc)
            .ToListAsync(ct);
}

internal sealed class NotificationTypeConfigRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<NotificationTypeConfig>(dbContext, currentUserService), INotificationTypeConfigRepository
{
    public async Task<List<NotificationTypeConfig>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<NotificationTypeConfig>()
            .Where(c => c.RailroadCtrlNbr == railroadCtrlNbr)
            .OrderBy(c => c.DisplayName)
            .ToListAsync(ct);

    public async Task<NotificationTypeConfig?> GetByRailroadAndKeyAsync(ControlNumber railroadCtrlNbr, string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var normalized = key.Trim();

        return await DbContext.Set<NotificationTypeConfig>()
            .SingleOrDefaultAsync(c => c.RailroadCtrlNbr == railroadCtrlNbr && c.Key == normalized, ct);
    }
}

internal sealed class PositionChangeRecordRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PositionChangeRecord>(dbContext, currentUserService), IPositionChangeRecordRepository
{
    public async Task<List<PositionChangeRecord>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<PositionChangeRecord>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(r => r.OpenedAtUtc)
            .ToListAsync(ct);

    public async Task<List<PositionChangeRecord>> GetOpenByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<PositionChangeRecord>()
            .Where(r => r.EmployeeCtrlNbr == employeeCtrlNbr && r.IsOpen)
            .OrderByDescending(r => r.OpenedAtUtc)
            .ToListAsync(ct);

    public async Task<List<PositionChangeRecord>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<PositionChangeRecord>()
            .Where(r => r.RailroadCtrlNbr == railroadCtrlNbr)
            .OrderByDescending(r => r.OpenedAtUtc)
            .ToListAsync(ct);

    public async Task<List<PositionChangeRecord>> GetOpenByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<PositionChangeRecord>()
            .Where(r => r.RailroadCtrlNbr == railroadCtrlNbr && r.IsOpen)
            .OrderByDescending(r => r.OpenedAtUtc)
            .ToListAsync(ct);

    public async Task<List<PositionChangeRecord>> GetOpenBySourceAsync(string sourceType, ControlNumber sourceCtrlNbr, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            return [];

        var normalized = sourceType.Trim();

        return await DbContext.Set<PositionChangeRecord>()
            .Where(r => r.SourceType == normalized && r.SourceCtrlNbr == sourceCtrlNbr && r.IsOpen)
            .OrderByDescending(r => r.OpenedAtUtc)
            .ToListAsync(ct);
    }
}
