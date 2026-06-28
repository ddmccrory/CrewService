using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Safety;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

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

internal sealed class SafetyObservationResolutionRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SafetyObservationResolution>(dbContext, currentUserService), ISafetyObservationResolutionRepository
{
    public async Task<SafetyObservationResolution?> GetByObservationAsync(
        ControlNumber observationCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<SafetyObservationResolution>()
            .SingleOrDefaultAsync(r => r.ObservationCtrlNbr == observationCtrlNbr, ct);
}

internal sealed class SafetyCategoryRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SafetyCategory>(dbContext, currentUserService), ISafetyCategoryRepository
{
    public async Task<IReadOnlyList<SafetyCategory>> GetByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<SafetyCategory>()
            .Where(c => c.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && c.IsActive)
            .ToListAsync(ct);
}
