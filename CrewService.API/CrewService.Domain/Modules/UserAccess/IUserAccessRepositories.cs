using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.UserAccess;

namespace CrewService.Domain.Modules.UserAccess;

public interface IUserParentAssignmentRepository : IRepository<UserParentAssignment>
{
    Task<List<UserParentAssignment>> GetByUserIdAsync(string userId);
    Task<List<UserParentAssignment>> GetByParentCtrlNbrAsync(long parentCtrlNbr);
    Task<UserParentAssignment?> GetByUserAndParentAsync(string userId, long parentCtrlNbr);
}
