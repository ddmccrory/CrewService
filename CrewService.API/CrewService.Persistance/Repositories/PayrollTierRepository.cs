using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class PayrollTierRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<PayrollTier>(dbContext, currentUserService), IPayrollTierRepository
{
    public async Task<List<PayrollTier>> GetByDynamicGroupCtrlNbrAsync(ControlNumber dynamicGroupCtrlNbr)
    {
        return await DbContext.Set<PayrollTier>()
            .Where(t => t.DynamicGroupCtrlNbr == dynamicGroupCtrlNbr)
            .ToListAsync();
    }
}
