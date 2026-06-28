using CrewService.Application.ElectronicCalling;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.VacancyCalls;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class VacancyCallRequestRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<VacancyCallRequest>(dbContext, currentUserService), IVacancyCallRequestRepository
{
    public override async Task<VacancyCallRequest?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<VacancyCallRequest>()
            .Include(r => r.Responses)
            .SingleOrDefaultAsync(r => r.CtrlNbr == ctrlNbr, ct);

    public async Task<IReadOnlyList<VacancyCallRequest>> GetPendingAsync(CancellationToken ct = default) =>
        await DbContext.Set<VacancyCallRequest>()
            .Where(r => r.Status == "Sent")
            .OrderBy(r => r.SentAtUtc)
            .ToListAsync(ct);
}
