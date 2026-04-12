using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class SeniorityStateRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SeniorityState>(dbContext, currentUserService), ISeniorityStateRepository
{
    public async Task<List<SeniorityState>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr)
    {
        return await DbContext.Set<SeniorityState>()
            .Where(s => s.ParentCtrlNbr == parentCtrlNbr)
            .OrderBy(s => s.StateDescription)
            .ToListAsync();
    }
}