using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Bulletins;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Bulletins;

internal sealed class PositionVacancyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PositionVacancy>(dbContext, currentUserService), IPositionVacancyRepository
{
    public async Task<List<PositionVacancy>> GetOpenAsync() =>
        await DbContext.Set<PositionVacancy>().Where(v => v.Status == "Open").ToListAsync();

    public async Task<List<PositionVacancy>> GetByTargetAsync(string targetType, ControlNumber targetCtrlNbr) =>
        await DbContext.Set<PositionVacancy>()
            .Where(v => v.TargetType == targetType && v.TargetCtrlNbr == targetCtrlNbr)
            .ToListAsync();

    public async Task<List<PositionVacancy>> GetByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<PositionVacancy>().Where(v => v.CraftCtrlNbr == craftCtrlNbr).ToListAsync();
}

internal sealed class BulletinRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Bulletin>(dbContext, currentUserService), IBulletinRepository
{
    public async Task<Bulletin?> GetByVacancyAsync(ControlNumber positionVacancyCtrlNbr) =>
        await DbContext.Set<Bulletin>().SingleOrDefaultAsync(b => b.PositionVacancyCtrlNbr == positionVacancyCtrlNbr);

    public async Task<List<Bulletin>> GetPostedAsync() =>
        await DbContext.Set<Bulletin>().Where(b => b.Status == "Posted").ToListAsync();

    public async Task<List<Bulletin>> GetPostedByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<Bulletin>()
            .Where(b => b.CraftCtrlNbr == craftCtrlNbr && b.Status == "Posted")
            .ToListAsync();

    public async Task<List<Bulletin>> GetByStatusAsync(string status) =>
        await DbContext.Set<Bulletin>().Where(b => b.Status == status).ToListAsync();
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
