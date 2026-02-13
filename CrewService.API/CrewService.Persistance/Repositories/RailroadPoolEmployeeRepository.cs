using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Railroads;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class RailroadPoolEmployeeRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<RailroadPoolEmployee>(dbContext, currentUserService), IRailroadPoolEmployeeRepository
{
    public async Task<List<RailroadPoolEmployee>> GetByRailroadPoolCtrlNbrAsync(ControlNumber railroadPoolCtrlNbr)
    {
        return await DbContext.Set<RailroadPoolEmployee>()
            .Where(rpe => rpe.RailroadPoolCtrlNbr == railroadPoolCtrlNbr)
            .ToListAsync();
    }

    public async Task<List<RailroadPoolEmployee>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
    {
        return await DbContext.Set<RailroadPoolEmployee>()
            .Where(rpe => rpe.EmployeeCtrlNbr == employeeCtrlNbr)
            .ToListAsync();
    }
}