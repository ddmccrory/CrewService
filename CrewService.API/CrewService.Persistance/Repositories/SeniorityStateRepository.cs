using CrewService.Domain.Interfaces;
using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.Seniority;
using CrewService.Persistance.Data;

namespace CrewService.Persistance.Repositories;

internal sealed class SeniorityStateRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<SeniorityState>(dbContext, currentUserService), ISeniorityStateRepository
{
    // All CRUD inherited from base Repository<SeniorityState>
}