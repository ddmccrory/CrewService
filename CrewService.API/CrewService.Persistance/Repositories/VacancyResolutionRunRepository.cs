using CrewService.Application.VacancyAssignment;
using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Dispatching;
using CrewService.Persistance.Data;
using CrewService.Persistance.Repositories;

namespace CrewService.Persistance.Repositories;

internal sealed class VacancyResolutionRunRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<VacancyResolutionRun>(dbContext, currentUserService), IVacancyResolutionRunRepository
{
}
