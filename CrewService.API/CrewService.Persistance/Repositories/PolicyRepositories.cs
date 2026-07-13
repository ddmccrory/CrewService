using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

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

internal sealed class CallSheetRuleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<CallSheetRule>(dbContext, currentUserService), ICallSheetRuleRepository
{
    public async Task<CallSheetRule?> GetByDepartmentAsync(ControlNumber departmentCtrlNbr) =>
        await DbContext.Set<CallSheetRule>().SingleOrDefaultAsync(r => r.DepartmentCtrlNbr == departmentCtrlNbr);

    public async Task<List<CallSheetRule>> GetByDepartmentsAsync(IEnumerable<ControlNumber> departmentCtrlNbrs)
    {
        var ctrlNbrs = departmentCtrlNbrs.ToList();
        if (ctrlNbrs.Count == 0)
            return [];

        return await DbContext.Set<CallSheetRule>()
            .Where(r => ctrlNbrs.Contains(r.DepartmentCtrlNbr))
            .ToListAsync();
    }
}

internal sealed class DepartmentReassignmentRuleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<DepartmentReassignmentRule>(dbContext, currentUserService), IDepartmentReassignmentRuleRepository
{
    public async Task<DepartmentReassignmentRule?> GetByDepartmentAsync(ControlNumber departmentCtrlNbr) =>
        await DbContext.Set<DepartmentReassignmentRule>().SingleOrDefaultAsync(r => r.DepartmentCtrlNbr == departmentCtrlNbr);
}

internal sealed class SeniorityMovePolicyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SeniorityMovePolicy>(dbContext, currentUserService), ISeniorityMovePolicyRepository
{
    public async Task<SeniorityMovePolicy?> GetByRailroadAndCraftAsync(ControlNumber railroadCtrlNbr, ControlNumber craftCtrlNbr) =>
        await DbContext.Set<SeniorityMovePolicy>().SingleOrDefaultAsync(p => p.RailroadCtrlNbr == railroadCtrlNbr && p.CraftCtrlNbr == craftCtrlNbr);

    public async Task<List<SeniorityMovePolicy>> GetByRailroadAsync(ControlNumber railroadCtrlNbr) =>
        await DbContext.Set<SeniorityMovePolicy>().Where(p => p.RailroadCtrlNbr == railroadCtrlNbr).ToListAsync();
}

internal sealed class NoAccessPolicyRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<NoAccessPolicy>(dbContext, currentUserService), INoAccessPolicyRepository
{
    public async Task<NoAccessPolicy?> GetByRailroadAndCraftAsync(ControlNumber railroadCtrlNbr, ControlNumber craftCtrlNbr) =>
        await DbContext.Set<NoAccessPolicy>().SingleOrDefaultAsync(p => p.RailroadCtrlNbr == railroadCtrlNbr && p.CraftCtrlNbr == craftCtrlNbr);

    public async Task<List<NoAccessPolicy>> GetByRailroadAsync(ControlNumber railroadCtrlNbr) =>
        await DbContext.Set<NoAccessPolicy>().Where(p => p.RailroadCtrlNbr == railroadCtrlNbr).ToListAsync();
}

internal sealed class SeniorityMoveRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SeniorityMove>(dbContext, currentUserService), ISeniorityMoveRepository
{
    public async Task<List<SeniorityMove>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(m => m.RequestedUtc)
            .ToListAsync(ct);

    public async Task<List<SeniorityMove>> GetByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.CraftCtrlNbr == craftCtrlNbr)
            .OrderByDescending(m => m.RequestedUtc)
            .ToListAsync(ct);

    public async Task<List<SeniorityMove>> GetByStatusAsync(string status, CancellationToken ct = default) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.Status == status)
            .OrderBy(m => m.RequestedUtc)
            .ToListAsync(ct);

    public async Task<List<SeniorityMove>> GetByCraftByStatusAsync(ControlNumber craftCtrlNbr, string status, CancellationToken ct = default) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.CraftCtrlNbr == craftCtrlNbr && m.Status == status)
            .OrderBy(m => m.RequestedUtc)
            .ToListAsync(ct);

    public async Task<List<SeniorityMove>> GetPendingAsync(CancellationToken ct = default) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.Status == SeniorityMoveStatus.Pending)
            .OrderBy(m => m.RequestedUtc)
            .ToListAsync(ct);

    public async Task<List<SeniorityMove>> GetActiveAsync(CancellationToken ct = default) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.Status == SeniorityMoveStatus.Pending || m.Status == SeniorityMoveStatus.Approved)
            .OrderBy(m => m.RequestedUtc)
            .ToListAsync(ct);

    public async Task<List<SeniorityMove>> GetAllMovesAsync(CancellationToken ct = default) =>
        await DbContext.Set<SeniorityMove>()
            .OrderByDescending(m => m.RequestedUtc)
            .ToListAsync(ct);

    public async Task<List<SeniorityMove>> GetApprovedDueAsync(DateTime asOf, CancellationToken ct = default) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.Status == SeniorityMoveStatus.Approved && m.EffectiveUtc <= asOf)
            .OrderBy(m => m.EffectiveUtc)
            .ToListAsync(ct);

    public async Task<DateTime?> GetNextApprovedEffectiveUtcAsync(CancellationToken ct = default) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.Status == SeniorityMoveStatus.Approved && m.EffectiveUtc != null)
            .MinAsync(m => (DateTime?)m.EffectiveUtc, ct);

    public async Task<List<SeniorityMove>> GetPendingByTargetPositionAsync(
        ControlNumber targetPositionCtrlNbr, ControlNumber excludeCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<SeniorityMove>()
            .Where(m => m.TargetPositionCtrlNbr == targetPositionCtrlNbr
                     && m.Status == SeniorityMoveStatus.Pending
                     && m.CtrlNbr != excludeCtrlNbr)
            .OrderBy(m => m.RequestedUtc)
            .ToListAsync(ct);
}
