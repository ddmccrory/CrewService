using CrewService.Application.ElectronicCalling;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Notifications;

internal sealed class NotificationRequestRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<NotificationRequest>(dbContext, currentUserService), INotificationRequestRepository
{
    public override async Task<NotificationRequest?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<NotificationRequest>()
            .Include(r => r.Responses)
            .SingleOrDefaultAsync(r => r.CtrlNbr == ctrlNbr, ct);

    public async Task<IReadOnlyList<NotificationRequest>> GetPendingAsync(CancellationToken ct = default) =>
        await DbContext.Set<NotificationRequest>()
            .Where(r => r.Status == "Sent")
            .OrderBy(r => r.SentAtUtc)
            .ToListAsync(ct);
}
