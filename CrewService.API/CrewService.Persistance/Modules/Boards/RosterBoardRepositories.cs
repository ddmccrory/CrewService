using CrewService.Application.RosterBoardOps;
using CrewService.Domain.Interfaces;
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
    public override async Task<RosterBoard?> GetByCtrlNbrAsync(ControlNumber ctrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<RosterBoard>()
            .Include(b => b.Positions)
            .SingleOrDefaultAsync(b => b.CtrlNbr == ctrlNbr, ct);

    public async Task<IReadOnlyList<RosterBoard>> GetActiveByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<RosterBoard>()
            .Include(b => b.Positions)
            .Where(b => b.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && b.IsActive)
            .ToListAsync(ct);
}

internal sealed class DailyEmployeeStatusRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DailyEmployeeStatusRecord>(dbContext, currentUserService), IDailyEmployeeStatusRepository
{
}
