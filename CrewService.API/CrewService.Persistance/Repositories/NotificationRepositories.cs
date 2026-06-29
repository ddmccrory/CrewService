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
}
