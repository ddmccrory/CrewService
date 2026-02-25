using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class SeniorityRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<Seniority>(dbContext, currentUserService), ISeniorityRepository
{
    public async Task<List<Seniority>> GetByRosterCtrlNbrAsync(ControlNumber rosterCtrlNbr)
    {
        return await DbContext.Set<Seniority>()
            .Where(s => s.RosterCtrlNbr == rosterCtrlNbr)
            .OrderBy(s => s.Rank)
            .ToListAsync();
    }

    public async Task<List<Seniority>> GetByEmployeeCtrlNbrAsync(ControlNumber employeeCtrlNbr)
    {
        return await DbContext.Set<Seniority>()
            .Where(s => s.EmployeeCtrlNbr == employeeCtrlNbr)
            .OrderByDescending(s => s.RosterDate)
            .ToListAsync();
    }
}