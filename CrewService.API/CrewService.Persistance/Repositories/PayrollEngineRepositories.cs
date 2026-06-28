using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Payroll;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class EarningCodeRuleRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<EarningCodeRule>(dbContext, currentUserService), IEarningCodeRuleRepository
{
    public async Task<IReadOnlyList<EarningCodeRule>> GetActiveByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default) =>
        await DbContext.Set<EarningCodeRule>()
            .Where(r => r.WorkAreaGroupCtrlNbr == workAreaGroupCtrlNbr && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);
}

internal sealed class PayRateRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PayRate>(dbContext, currentUserService), IPayRateRepository
{
    public async Task<PayRate?> GetEffectiveAsync(
        ControlNumber craftCtrlNbr, DateTime asOfDate,
        ControlNumber? craftRoleCtrlNbr = null, CancellationToken ct = default)
    {
        var query = DbContext.Set<PayRate>()
            .Where(r => r.CraftCtrlNbr == craftCtrlNbr && r.EffectiveDate <= asOfDate);

        if (craftRoleCtrlNbr is not null)
            query = query.Where(r => r.CraftRoleCtrlNbr == craftRoleCtrlNbr);
        else
            query = query.Where(r => r.CraftRoleCtrlNbr == null);

        return await query.OrderByDescending(r => r.EffectiveDate).FirstOrDefaultAsync(ct);
    }
}
