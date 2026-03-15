using CrewService.Domain.Interfaces;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.Modules.UserAccess;
using CrewService.Domain.ValueObjects;
using CrewService.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Persistance.Repositories;

internal sealed class UserParentAssignmentRepository(CrewServiceDbContext dbContext, ICurrentUserService currentUserService)
    : Repository<UserParentAssignment>(dbContext, currentUserService), IUserParentAssignmentRepository
{
    public async Task<List<UserParentAssignment>> GetByUserIdAsync(string userId)
    {
        return await DbContext.Set<UserParentAssignment>()
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<UserParentAssignment>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr)
    {
        return await DbContext.Set<UserParentAssignment>()
            .Where(a => a.ParentCtrlNbr == parentCtrlNbr)
            .ToListAsync();
    }

    public async Task<UserParentAssignment?> GetByUserAndParentAsync(string userId, ControlNumber parentCtrlNbr)
    {
        return await DbContext.Set<UserParentAssignment>()
            .SingleOrDefaultAsync(a => a.UserId == userId && a.ParentCtrlNbr == parentCtrlNbr);
    }
}
