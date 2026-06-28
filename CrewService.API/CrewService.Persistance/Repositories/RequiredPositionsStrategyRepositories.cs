using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RequiredPositionsStrategyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RequiredPositionsStrategy>(dbContext, currentUserService), IRequiredPositionsStrategyRepository
{
    public async Task<RequiredPositionsStrategy?> GetStaticAsync(CancellationToken ct = default) =>
        await DbContext.Set<RequiredPositionsStrategy>()
            .SingleOrDefaultAsync(s => s.Code == "STATIC", ct);

    public async Task<List<RequiredPositionsStrategy>> GetAllSystemStrategiesAsync(CancellationToken ct = default) =>
        await DbContext.Set<RequiredPositionsStrategy>()
            .OrderBy(s => s.Code)
            .ToListAsync(ct);

    public async Task<RequiredPositionsStrategy?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        await DbContext.Set<RequiredPositionsStrategy>()
            .SingleOrDefaultAsync(s => s.Code == code.ToUpperInvariant(), ct);
}

internal sealed class CraftRequiredPositionsStrategyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CraftRequiredPositionsStrategy>(dbContext, currentUserService), ICraftRequiredPositionsStrategyRepository
{
    public async Task<CraftRequiredPositionsStrategy?> GetByCraftAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<CraftRequiredPositionsStrategy>()
            .SingleOrDefaultAsync(cs => cs.CraftCtrlNbr == craftCtrlNbr, ct);

    public async Task<List<CraftRequiredPositionsStrategy>> GetByCraftsAsync(
        IEnumerable<ControlNumber> craftCtrlNbrs, CancellationToken ct = default)
    {
        var ids = craftCtrlNbrs.ToList();
        return await DbContext.Set<CraftRequiredPositionsStrategy>()
            .Where(cs => ids.Contains(cs.CraftCtrlNbr))
            .ToListAsync(ct);
    }

    public async Task<List<CraftRequiredPositionsStrategy>> GetByStrategyCtrlNbrsAsync(
        IEnumerable<ControlNumber> strategyCtrlNbrs, CancellationToken ct = default)
    {
        var ids = strategyCtrlNbrs.ToList();
        return await DbContext.Set<CraftRequiredPositionsStrategy>()
            .Where(cs => ids.Contains(cs.StrategyCtrlNbr))
            .ToListAsync(ct);
    }
}
