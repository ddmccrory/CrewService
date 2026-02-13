using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RailroadRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Railroad>(dbContext, currentUserService), IRailroadRepository
{
    public async Task<List<Railroad>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr)
    {
        return await DbContext.Set<Railroad>()
            .Where(r => r.ParentCtrlNbr == parentCtrlNbr)
            .OrderBy(r => r.RailroadMark)
            .ToListAsync();
    }
}