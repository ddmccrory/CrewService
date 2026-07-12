using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class SeniorityStateTypeVacancyDefaultRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SeniorityStateTypeVacancyDefault>(dbContext, currentUserService), ISeniorityStateTypeVacancyDefaultRepository
{
    public async Task<List<SeniorityStateTypeVacancyDefault>> GetByRailroadCtrlNbrAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<SeniorityStateTypeVacancyDefault>()
            .Where(c => c.RailroadCtrlNbr == railroadCtrlNbr)
            .OrderBy(c => c.StateType)
            .ToListAsync(ct);
    }

    public async Task<SeniorityStateTypeVacancyDefault?> GetByStateTypeAsync(ControlNumber railroadCtrlNbr, StateType stateType, CancellationToken ct = default)
    {
        return await DbContext.Set<SeniorityStateTypeVacancyDefault>()
            .FirstOrDefaultAsync(c => c.RailroadCtrlNbr == railroadCtrlNbr && c.StateType == stateType, ct);
    }
}
