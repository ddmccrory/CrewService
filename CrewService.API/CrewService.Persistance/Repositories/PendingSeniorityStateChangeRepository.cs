using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class PendingSeniorityStateChangeRepository(
    CrewServiceDbContext context,
    ICurrentUserService currentUserService)
    : Repository<PendingSeniorityStateChange>(context, currentUserService),
      IPendingSeniorityStateChangeRepository
{
    public Task<List<PendingSeniorityStateChange>> GetAllPendingByRailroadAsync(
        ControlNumber railroadCtrlNbr, CancellationToken ct = default)
        => (from p in DbContext.PendingSeniorityStateChanges
            join s in DbContext.Set<Seniority>() on p.SeniorityCtrlNbr equals s.CtrlNbr
            join r in DbContext.Set<Roster>() on s.RosterCtrlNbr equals r.CtrlNbr
            join c in DbContext.Set<Craft>() on r.CraftCtrlNbr equals c.CtrlNbr
            where p.Status == PendingStateChangeStatus.Pending
               && c.DynamicGroupCtrlNbr == railroadCtrlNbr
            orderby p.EffectiveDateUtc
            select p)
           .ToListAsync(ct);

    public Task<PendingSeniorityStateChange?> GetPendingByEmployeeAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
        => DbContext.PendingSeniorityStateChanges
            .FirstOrDefaultAsync(
                p => p.EmployeeCtrlNbr == employeeCtrlNbr
                  && p.Status == PendingStateChangeStatus.Pending,
                ct);

    public Task<List<PendingSeniorityStateChange>> GetDueAsync(
        DateTime asOfUtc, CancellationToken ct = default)
        => DbContext.PendingSeniorityStateChanges
            .Where(p => p.Status == PendingStateChangeStatus.Pending
                     && p.EffectiveDateUtc <= asOfUtc)
            .OrderBy(p => p.EffectiveDateUtc)
            .ToListAsync(ct);

    public async Task<DateTime?> GetNextEffectiveDateUtcAsync(CancellationToken ct = default)
        => await DbContext.PendingSeniorityStateChanges
            .Where(p => p.Status == PendingStateChangeStatus.Pending)
            .MinAsync(p => (DateTime?)p.EffectiveDateUtc, ct);
}
