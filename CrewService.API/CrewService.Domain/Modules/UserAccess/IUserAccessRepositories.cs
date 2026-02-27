using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.UserAccess;

namespace CrewService.Domain.Modules.UserAccess;

public interface IUserParentAssignmentRepository : IRepository<UserParentAssignment>
{
    Task<List<UserParentAssignment>> GetByUserIdAsync(string userId);
    Task<List<UserParentAssignment>> GetByParentCtrlNbrAsync(long parentCtrlNbr);
    Task<UserParentAssignment?> GetByUserAndParentAsync(string userId, long parentCtrlNbr);
}

public interface IInvitationRepository : IRepository<Invitation>
{
    Task<Invitation?> GetByTokenAsync(string token);
    Task<List<Invitation>> GetByEmailAsync(string email);
    Task<List<Invitation>> GetByParentCtrlNbrAsync(long parentCtrlNbr);
    Task<Invitation?> GetPendingByEmailAndParentAsync(string email, long parentCtrlNbr);
}
