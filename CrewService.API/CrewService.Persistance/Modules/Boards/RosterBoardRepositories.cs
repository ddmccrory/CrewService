using CrewService.Application.RosterBoardOps;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Boards;

internal sealed class RosterBoardRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RosterBoard>(dbContext, currentUserService), IRosterBoardRepository
{
    public override async Task<List<RosterBoard>> GetAllAsync(CancellationToken ct = default) =>
        await DbContext.Set<RosterBoard>()
            .Include(b => b.Positions)
            .ToListAsync(ct);

    public override async Task<RosterBoard?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<RosterBoard>()
            .Include(b => b.Positions)
            .SingleOrDefaultAsync(b => b.CtrlNbr == ctrlNbr, ct);

    public async Task<IReadOnlyList<RosterBoard>> GetActiveByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await (from b in DbContext.Set<RosterBoard>().Include(b => b.Positions)
               join r in DbContext.Set<Roster>() on b.RosterCtrlNbr equals r.CtrlNbr
               where r.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && b.IsActive
               select b)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<RosterBoard>()
            .Include(b => b.Positions)
            .Where(b => b.CraftCtrlNbr == craftCtrlNbr)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RosterBoard>> GetByCraftCtrlNbrsAsync(
        IEnumerable<ControlNumber> craftCtrlNbrs, CancellationToken ct = default)
    {
        var ctrlNbrs = craftCtrlNbrs.ToList();
        return await DbContext.Set<RosterBoard>()
            .Include(b => b.Positions)
            .Where(b => ctrlNbrs.Contains(b.CraftCtrlNbr))
            .ToListAsync(ct);
    }
}

internal sealed class DailyEmployeeStatusRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DailyEmployeeStatusRecord>(dbContext, currentUserService), IDailyEmployeeStatusRepository
{
}
