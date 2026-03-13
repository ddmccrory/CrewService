using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Safety;

internal sealed class SafetyObservationRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SafetyObservation>(dbContext, currentUserService), ISafetyObservationRepository
{
    public override async Task<SafetyObservation?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<SafetyObservation>()
            .Include(o => o.Actions)
            .SingleOrDefaultAsync(o => o.CtrlNbr == ctrlNbr, ct);

    public async Task<IReadOnlyList<SafetyObservation>> GetByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<SafetyObservation>()
            .Include(o => o.Actions)
            .Where(o => o.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr)
            .OrderByDescending(o => o.ObservedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SafetyObservation>> GetOpenByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<SafetyObservation>()
            .Include(o => o.Actions)
            .Where(o => o.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && o.Status != "Resolved")
            .OrderByDescending(o => o.ObservedAtUtc)
            .ToListAsync(ct);
}
