using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Policies;

public interface ICraftDisplacementPolicyRepository : IRepository<CraftDisplacementPolicy>
{
    Task<CraftDisplacementPolicy?> GetByCraftAsync(ControlNumber craftCtrlNbr);
}

public interface IDisplacementCaseRepository : IRepository<DisplacementCase>
{
    Task<List<DisplacementCase>> GetOpenByEmployeeAsync(ControlNumber employeeCtrlNbr);
    Task<List<DisplacementCase>> GetByCraftAsync(ControlNumber craftCtrlNbr);
}

public interface IDisplacementClaimRepository : IRepository<DisplacementClaim>
{
    Task<List<DisplacementClaim>> GetByCaseAsync(ControlNumber caseCtrlNbr);
}

public interface IBulletinPolicyRepository : IRepository<BulletinPolicy>
{
    Task<BulletinPolicy?> GetByCraftAsync(ControlNumber craftCtrlNbr);
}

public interface ISeniorityMovePolicyRepository : IRepository<SeniorityMovePolicy>
{
    Task<SeniorityMovePolicy?> GetByCraftAsync(ControlNumber craftCtrlNbr);
}

public interface ISeniorityMoveRepository : IRepository<SeniorityMove>
{
    Task<List<SeniorityMove>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr);
    Task<List<SeniorityMove>> GetByCraftAsync(ControlNumber craftCtrlNbr);
}
