using CrewService.Domain.Models.Railroads;
using CrewService.Domain.Repositories;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RailroadRepository(CrewAssignmentDbContext dbContext)
    : Repository<Railroad>(dbContext), IRailroadRepository
{
    public async Task<List<Railroad>> GetAllRailroadsByParentCtrlNbrAsync(long parentCtrlNbr)
    {
        return await DbContext.Set<Railroad>()
                              .Where(rr => rr.ParentCtrlNbr == ControlNumber.Create(parentCtrlNbr))
                              .OrderBy(rr => rr.RailroadMark).ToListAsync();
    }
}