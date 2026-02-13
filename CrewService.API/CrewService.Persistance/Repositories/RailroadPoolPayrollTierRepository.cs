using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RailroadPoolPayrollTierRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RailroadPoolPayrollTier>(dbContext, currentUserService), IRailroadPoolPayrollTierRepository
{
    public async Task<List<RailroadPoolPayrollTier>> GetByRailroadPoolCtrlNbrAsync(ControlNumber railroadPoolCtrlNbr)
    {
        return await DbContext.Set<RailroadPoolPayrollTier>()
            .Where(t => t.RailroadPoolCtrlNbr == railroadPoolCtrlNbr)
            .ToListAsync();
    }
}