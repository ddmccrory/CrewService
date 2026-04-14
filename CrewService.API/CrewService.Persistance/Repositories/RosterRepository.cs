using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RosterRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Roster>(dbContext, currentUserService), IRosterRepository
{
    public async Task<List<Roster>> GetByCraftCtrlNbrAsync(ControlNumber craftCtrlNbr)
    {
        return await DbContext.Set<Roster>()
            .Where(r => r.CraftCtrlNbr == craftCtrlNbr)
            .OrderBy(r => r.RosterNumber)
            .ToListAsync();
    }

    public async Task<List<Roster>> GetByCraftCtrlNbrsAsync(IEnumerable<ControlNumber> craftCtrlNbrs)
    {
        var ctrlNbrs = craftCtrlNbrs.ToList();
        return await DbContext.Set<Roster>()
            .Where(r => ctrlNbrs.Contains(r.CraftCtrlNbr))
            .OrderBy(r => r.RosterNumber)
            .ToListAsync();
    }
}