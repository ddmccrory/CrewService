using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.Models.UserAccess;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.UserAccess;

public interface IUserParentAssignmentRepository : IRepository<UserParentAssignment>
{
    Task<List<UserParentAssignment>> GetByUserIdAsync(string userId);
    Task<List<UserParentAssignment>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr);
    Task<List<UserParentAssignment>> GetByUserAndParentAsync(string userId, ControlNumber parentCtrlNbr);
}

public interface IInvitationRepository : IRepository<Invitation>
{
    Task<Invitation?> GetByTokenAsync(string token);
    Task<List<Invitation>> GetByEmailAsync(string email);
    Task<List<Invitation>> GetByParentCtrlNbrAsync(ControlNumber parentCtrlNbr);
    Task<Invitation?> GetPendingByEmailAndParentAsync(string email, ControlNumber parentCtrlNbr);
    Task<List<Invitation>> GetAcceptedByEmailAndParentAsync(string email, ControlNumber parentCtrlNbr);
}
