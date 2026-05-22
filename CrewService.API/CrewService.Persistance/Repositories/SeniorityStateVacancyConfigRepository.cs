using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class SeniorityStateVacancyConfigRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SeniorityStateVacancyConfig>(dbContext, currentUserService), ISeniorityStateVacancyConfigRepository
{
    public async Task<List<SeniorityStateVacancyConfig>> GetByRailroadCtrlNbrAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<SeniorityStateVacancyConfig>()
            .Where(c => c.RailroadCtrlNbr == railroadCtrlNbr)
            .OrderBy(c => c.SeniorityStateCtrlNbr)
            .ToListAsync(ct);
    }

    public async Task<SeniorityStateVacancyConfig?> GetBySeniorityStateAsync(ControlNumber railroadCtrlNbr, ControlNumber seniorityStateCtrlNbr, CancellationToken ct = default)
    {
        return await DbContext.Set<SeniorityStateVacancyConfig>()
            .FirstOrDefaultAsync(c => c.RailroadCtrlNbr == railroadCtrlNbr && c.SeniorityStateCtrlNbr == seniorityStateCtrlNbr, ct);
    }
}
