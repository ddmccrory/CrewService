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
