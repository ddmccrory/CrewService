using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Modules.Policies;

internal sealed class CraftDisplacementPolicyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CraftDisplacementPolicy>(dbContext, currentUserService), ICraftDisplacementPolicyRepository
{
    public async Task<CraftDisplacementPolicy?> GetByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<CraftDisplacementPolicy>().SingleOrDefaultAsync(p => p.CraftCtrlNbr == craftCtrlNbr);
}

internal sealed class DisplacementCaseRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DisplacementCase>(dbContext, currentUserService), IDisplacementCaseRepository
{
    public async Task<List<DisplacementCase>> GetOpenByEmployeeAsync(ControlNumber employeeCtrlNbr) =>
        await DbContext.Set<DisplacementCase>()
            .Where(c => c.EmployeeCtrlNbr == employeeCtrlNbr && c.Status == "Open")
            .ToListAsync();

    public async Task<List<DisplacementCase>> GetByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<DisplacementCase>()
            .Where(c => c.CraftCtrlNbr == craftCtrlNbr)
            .ToListAsync();
}

internal sealed class DisplacementClaimRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DisplacementClaim>(dbContext, currentUserService), IDisplacementClaimRepository
{
    public async Task<List<DisplacementClaim>> GetByCaseAsync(ControlNumber caseCtrlNbr) =>
        await DbContext.Set<DisplacementClaim>()
            .Where(c => c.CaseCtrlNbr == caseCtrlNbr)
            .ToListAsync();
}

internal sealed class BulletinPolicyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<BulletinPolicy>(dbContext, currentUserService), IBulletinPolicyRepository
{
    public async Task<BulletinPolicy?> GetByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<BulletinPolicy>().SingleOrDefaultAsync(p => p.CraftCtrlNbr == craftCtrlNbr);
}

internal sealed class SeniorityMovePolicyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SeniorityMovePolicy>(dbContext, currentUserService), ISeniorityMovePolicyRepository
{
    public async Task<SeniorityMovePolicy?> GetByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<SeniorityMovePolicy>().SingleOrDefaultAsync(p => p.CraftCtrlNbr == craftCtrlNbr);
}

internal sealed class SeniorityMoveRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SeniorityMove>(dbContext, currentUserService), ISeniorityMoveRepository
{
    public async Task<List<SeniorityMove>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(m => m.ExercisedUtc)
            .ToListAsync();

    public async Task<List<SeniorityMove>> GetByCraftAsync(ControlNumber craftCtrlNbr) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.CraftCtrlNbr == craftCtrlNbr)
            .ToListAsync();
}
