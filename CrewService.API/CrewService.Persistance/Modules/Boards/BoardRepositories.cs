using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Boards;

internal sealed class BoardCascadePolicyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<BoardCascadePolicy>(dbContext, currentUserService), IBoardCascadePolicyRepository
{
    public async Task<BoardCascadePolicy?> GetByWorkAreaAndCraftAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr) =>
        await DbContext.Set<BoardCascadePolicy>()
            .SingleOrDefaultAsync(p => p.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && p.CraftCtrlNbr == craftCtrlNbr);
}
