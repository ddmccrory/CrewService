using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.Modules.Staffing;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Bulletins;

internal sealed class PositionVacancyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PositionVacancy>(dbContext, currentUserService), IPositionVacancyRepository
{
    public async Task<List<PositionVacancy>> GetOpenAsync() =>
        await DbContext.Set<PositionVacancy>()
            .Where(v => v.Status == "Open" || v.Status == "Bulletined")
            .ToListAsync();

    public async Task<List<PositionVacancy>> GetOpenByRailroadAsync(ControlNumber railroadCtrlNbr) =>
        await DbContext.Set<PositionVacancy>()
            .Where(v => (v.Status == "Open" || v.Status == "Bulletined") &&
                        DbContext.Set<DynamicGroup>().Any(g => g.CtrlNbr == v.WorkAreaGroupCtrlNbr && g.RailroadCtrlNbr == railroadCtrlNbr))
            .ToListAsync();

    public async Task<List<PositionVacancy>> GetByTargetAsync(string targetType, ControlNumber targetCtrlNbr) =>
        await DbContext.Set<PositionVacancy>()
            .Where(v => v.TargetType == targetType && v.TargetCtrlNbr == targetCtrlNbr)
            .ToListAsync();

    public async Task<List<PositionVacancy>> GetByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<PositionVacancy>().Where(v => v.CraftCtrlNbr == craftCtrlNbr).ToListAsync();

    public async Task<List<PositionVacancy>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr) =>
        await DbContext.Set<PositionVacancy>().Where(v => v.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr).ToListAsync();

    public async Task<double> GetAverageDailyBoardVacanciesAsync(
        ControlNumber workAreaGroupCtrlNbr, ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var count = await DbContext.Set<PositionVacancy>()
            .CountAsync(v =>
                v.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr &&
                v.CraftCtrlNbr == craftCtrlNbr &&
                v.TargetType == StaffablePositionType.Board &&
                v.OpenedUtc >= since, ct);
        return count / 30.0;
    }
}

internal sealed class BulletinRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Bulletin>(dbContext, currentUserService), IBulletinRepository
{
    public async Task<Bulletin?> GetByVacancyAsync(ControlNumber positionVacancyCtrlNbr) =>
        await DbContext.Set<Bulletin>().SingleOrDefaultAsync(b => b.PositionVacancyCtrlNbr == positionVacancyCtrlNbr);

    public async Task<List<Bulletin>> GetPostedAsync() =>
        await DbContext.Set<Bulletin>().Where(b => b.Status == "Posted").ToListAsync();

    public async Task<List<Bulletin>> GetPostedByRailroadAsync(ControlNumber railroadCtrlNbr) =>
        await DbContext.Set<Bulletin>()
            .Where(b => b.Status == "Posted" &&
                        DbContext.Set<PositionVacancy>().Any(v => v.CtrlNbr == b.PositionVacancyCtrlNbr &&
                            DbContext.Set<DynamicGroup>().Any(g => g.CtrlNbr == v.WorkAreaGroupCtrlNbr && g.RailroadCtrlNbr == railroadCtrlNbr)))
            .ToListAsync();

    public async Task<List<Bulletin>> GetActiveAsync() =>
        await DbContext.Set<Bulletin>().Where(b => b.Status == "Posted" || b.Status == "NoBid").ToListAsync();

    public async Task<List<Bulletin>> GetActiveByRailroadAsync(ControlNumber railroadCtrlNbr) =>
        await DbContext.Set<Bulletin>()
            .Where(b => (b.Status == "Posted" || b.Status == "NoBid") &&
                        DbContext.Set<PositionVacancy>().Any(v => v.CtrlNbr == b.PositionVacancyCtrlNbr &&
                            DbContext.Set<DynamicGroup>().Any(g => g.CtrlNbr == v.WorkAreaGroupCtrlNbr && g.RailroadCtrlNbr == railroadCtrlNbr)))
            .ToListAsync();

    public async Task<List<Bulletin>> GetPostedByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<Bulletin>()
            .Where(b => b.CraftCtrlNbr == craftCtrlNbr && b.Status == "Posted")
            .ToListAsync();

    public async Task<List<Bulletin>> GetByStatusAsync(string status) =>
        await DbContext.Set<Bulletin>().Where(b => b.Status == status).ToListAsync();

    public async Task<List<Bulletin>> GetByWorkAreaAsync(ControlNumber workAreaGroupCtrlNbr) =>
        await DbContext.Set<Bulletin>()
            .Join(DbContext.Set<PositionVacancy>(),
                b => b.PositionVacancyCtrlNbr,
                v => v.CtrlNbr,
                (b, v) => new { Bulletin = b, Vacancy = v })
            .Where(x => x.Vacancy.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr)
            .Select(x => x.Bulletin)
            .ToListAsync();

    public async Task<List<Bulletin>> GetClosedUnawardedAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await DbContext.Set<Bulletin>()
            .Where(b => b.Status == "Posted"
                     && b.AwardedEmployeeCtrlNbr == null
                     && b.BidWindowClosesUtc <= now)
            .ToListAsync(ct);
    }

    public async Task<List<Bulletin>> GetNoBidPastDeadlineAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await DbContext.Set<Bulletin>()
            .Where(b => b.Status == "NoBid"
                     && b.AwardedEmployeeCtrlNbr == null
                     && b.ForceAssignDeadlineUtc != null
                     && b.ForceAssignDeadlineUtc <= now)
            .ToListAsync(ct);
    }

    public async Task<List<Bulletin>> GetInDateRangeAsync(DateTime fromUtc, ControlNumber? railroadCtrlNbr = null)
    {
        var query = DbContext.Set<Bulletin>().Where(b => b.BidWindowOpensUtc >= fromUtc);
        if (railroadCtrlNbr is not null)
            query = query.Where(b =>
                DbContext.Set<PositionVacancy>().Any(v => v.CtrlNbr == b.PositionVacancyCtrlNbr &&
                    DbContext.Set<DynamicGroup>().Any(g => g.CtrlNbr == v.WorkAreaGroupCtrlNbr && g.RailroadCtrlNbr == railroadCtrlNbr)));
        return await query.OrderByDescending(b => b.BidWindowOpensUtc).ToListAsync();
    }

    public async Task<DateTime?> GetNextPendingEventUtcAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Earliest future bid-window close among Posted bulletins
        var nextBidClose = await DbContext.Set<Bulletin>()
            .Where(b => b.Status == "Posted" && b.BidWindowClosesUtc > now)
            .MinAsync(b => (DateTime?)b.BidWindowClosesUtc, ct);

        // Earliest future force-assign deadline among NoBid bulletins
        var nextForceAssign = await DbContext.Set<Bulletin>()
            .Where(b => b.Status == "NoBid" && b.ForceAssignDeadlineUtc != null && b.ForceAssignDeadlineUtc > now)
            .MinAsync(b => b.ForceAssignDeadlineUtc, ct);

        if (nextBidClose is null) return nextForceAssign;
        if (nextForceAssign is null) return nextBidClose;
        return nextBidClose < nextForceAssign ? nextBidClose : nextForceAssign;
    }

    public async Task<Bulletin?> GetNextPendingEventBulletinAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var nextBidClose = await DbContext.Set<Bulletin>()
            .Where(b => b.Status == "Posted" && b.BidWindowClosesUtc > now)
            .OrderBy(b => b.BidWindowClosesUtc)
            .FirstOrDefaultAsync(ct);

        var nextForceAssign = await DbContext.Set<Bulletin>()
            .Where(b => b.Status == "NoBid" && b.ForceAssignDeadlineUtc != null && b.ForceAssignDeadlineUtc > now)
            .OrderBy(b => b.ForceAssignDeadlineUtc)
            .FirstOrDefaultAsync(ct);

        if (nextBidClose is null) return nextForceAssign;
        if (nextForceAssign is null) return nextBidClose;
        return nextBidClose.BidWindowClosesUtc <= nextForceAssign.ForceAssignDeadlineUtc
            ? nextBidClose
            : nextForceAssign;
    }
}

internal sealed class BulletinBidRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<BulletinBid>(dbContext, currentUserService), IBulletinBidRepository
{
    public async Task<List<BulletinBid>> GetByBulletinAsync(ControlNumber bulletinCtrlNbr) =>
        await DbContext.Set<BulletinBid>()
            .Where(b => b.BulletinCtrlNbr == bulletinCtrlNbr)
            .OrderBy(b => b.SeniorityRank)
            .ToListAsync();

    public async Task<List<BulletinBid>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr) =>
        await DbContext.Set<BulletinBid>().Where(b => b.EmployeeCtrlNbr == employeeCtrlNbr).ToListAsync();

    public async Task<List<BulletinBid>> GetActiveByEmployeeAsync(ControlNumber employeeCtrlNbr) =>
        await DbContext.Set<BulletinBid>()
            .Where(b => b.EmployeeCtrlNbr == employeeCtrlNbr && b.Status == "Submitted")
            .OrderBy(b => b.Priority)
            .ToListAsync();
}

internal sealed class BulletinRuleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<BulletinRule>(dbContext, currentUserService), IBulletinRuleRepository
{
    public async Task<BulletinRule?> GetByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<BulletinRule>().SingleOrDefaultAsync(r => r.CraftCtrlNbr == craftCtrlNbr);
}
