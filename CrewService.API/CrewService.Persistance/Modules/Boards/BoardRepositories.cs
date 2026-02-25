using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Boards;

internal sealed class ExtraBoardRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<ExtraBoard>(dbContext, currentUserService), IExtraBoardRepository
{
    public async Task<List<ExtraBoard>> GetByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<ExtraBoard>().Where(b => b.CraftCtrlNbr == craftCtrlNbr).OrderBy(b => b.Name).ToListAsync();

    public async Task<List<ExtraBoard>> GetByPlacedGroupAsync(ControlNumber placedGroupCtrlNbr) =>
        await DbContext.Set<ExtraBoard>().Where(b => b.PlacedGroupCtrlNbr == placedGroupCtrlNbr).OrderBy(b => b.Name).ToListAsync();

    public async Task<List<ExtraBoard>> GetByKindAsync(string boardKind) =>
        await DbContext.Set<ExtraBoard>().Where(b => b.BoardKind == boardKind).OrderBy(b => b.Name).ToListAsync();
}

internal sealed class BoardMemberRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<BoardMember>(dbContext, currentUserService), IBoardMemberRepository
{
    public async Task<List<BoardMember>> GetByBoardAsync(ControlNumber extraBoardCtrlNbr) =>
        await DbContext.Set<BoardMember>().Where(m => m.ExtraBoardCtrlNbr == extraBoardCtrlNbr).OrderBy(m => m.OrderIndex).ToListAsync();

    public async Task<List<BoardMember>> GetActiveByBoardAsync(ControlNumber extraBoardCtrlNbr, DateTime asOfUtc) =>
        await DbContext.Set<BoardMember>()
            .Where(m => m.ExtraBoardCtrlNbr == extraBoardCtrlNbr && m.StartUtc <= asOfUtc && (m.EndUtc == null || m.EndUtc > asOfUtc))
            .OrderBy(m => m.OrderIndex)
            .ToListAsync();
}

internal sealed class BoardCascadePolicyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<BoardCascadePolicy>(dbContext, currentUserService), IBoardCascadePolicyRepository
{
    public async Task<BoardCascadePolicy?> GetByWorkAreaAndCraftAsync(ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr) =>
        await DbContext.Set<BoardCascadePolicy>()
            .SingleOrDefaultAsync(p => p.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && p.CraftCtrlNbr == craftCtrlNbr);
}
