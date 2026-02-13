using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RailroadEmployeeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RailroadEmployee>(dbContext, currentUserService), IRailroadEmployeeRepository
{
    public async Task<List<RailroadEmployee>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
    {
        return await DbContext.Set<RailroadEmployee>()
            .Where(re => re.EmployeeCtrlNbr == employeeCtrlNbr)
            .ToListAsync();
    }

    public async Task<RailroadEmployee?> GetByEmployeeAndRailroadAsync(ControlNumber employeeCtrlNbr, ControlNumber railroadCtrlNbr)
    {
        return await DbContext.Set<RailroadEmployee>()
            .SingleOrDefaultAsync(re => re.EmployeeCtrlNbr == employeeCtrlNbr 
                                     && re.RailroadCtrlNbr == railroadCtrlNbr);
    }

    public async Task<List<RailroadEmployee>> GetByRailroadCtrlNbrAsync(ControlNumber railroadCtrlNbr)
    {
        return await DbContext.Set<RailroadEmployee>()
            .Where(re => re.RailroadCtrlNbr == railroadCtrlNbr)
            .ToListAsync();
    }
}