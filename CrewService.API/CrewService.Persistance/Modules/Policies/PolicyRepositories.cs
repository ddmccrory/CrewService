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
