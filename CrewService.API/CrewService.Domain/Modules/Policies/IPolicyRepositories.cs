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

public interface ICallSheetRuleRepository : IRepository<CallSheetRule>
{
    Task<CallSheetRule?> GetByDepartmentAsync(ControlNumber departmentCtrlNbr);
    Task<List<CallSheetRule>> GetByDepartmentsAsync(IEnumerable<ControlNumber> departmentCtrlNbrs);
}

public interface ICraftCallSheetRuleRepository : IRepository<CraftCallSheetRule>
{
    Task<CraftCallSheetRule?> GetByCraftAsync(ControlNumber craftCtrlNbr);
    Task<List<CraftCallSheetRule>> GetByCraftsAsync(IEnumerable<ControlNumber> craftCtrlNbrs);
}

public interface IAbsenceApprovalPolicyRepository : IRepository<AbsenceApprovalPolicy>
{
    Task<AbsenceApprovalPolicy?> GetByRailroadAsync(ControlNumber railroadCtrlNbr);
}

public interface IDepartmentReassignmentRuleRepository : IRepository<DepartmentReassignmentRule>
{
    Task<DepartmentReassignmentRule?> GetByDepartmentAsync(ControlNumber departmentCtrlNbr);
}

public interface ISeniorityMovePolicyRepository : IRepository<SeniorityMovePolicy>
{
    Task<SeniorityMovePolicy?> GetByRailroadAndCraftAsync(ControlNumber railroadCtrlNbr, ControlNumber craftCtrlNbr);
    Task<List<SeniorityMovePolicy>> GetByRailroadAsync(ControlNumber railroadCtrlNbr);
}

public interface INoAccessPolicyRepository : IRepository<NoAccessPolicy>
{
    Task<NoAccessPolicy?> GetByRailroadAndCraftAsync(ControlNumber railroadCtrlNbr, ControlNumber craftCtrlNbr);
    Task<List<NoAccessPolicy>> GetByRailroadAsync(ControlNumber railroadCtrlNbr);
}

public interface ISeniorityMoveRepository : IRepository<SeniorityMove>
{
    Task<List<SeniorityMove>> GetByEmployeeAsync(ControlNumber employeeCtrlNbr, CancellationToken ct = default);
    Task<List<SeniorityMove>> GetByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default);
    Task<List<SeniorityMove>> GetByStatusAsync(string status, CancellationToken ct = default);
    Task<List<SeniorityMove>> GetByCraftByStatusAsync(ControlNumber craftCtrlNbr, string status, CancellationToken ct = default);
    Task<List<SeniorityMove>> GetPendingAsync(CancellationToken ct = default);
    /// <summary>Returns all moves with status Pending or Approved (active moves).</summary>
    Task<List<SeniorityMove>> GetActiveAsync(CancellationToken ct = default);
    /// <summary>Returns all moves regardless of status.</summary>
    Task<List<SeniorityMove>> GetAllMovesAsync(CancellationToken ct = default);
    /// <summary>Returns all Approved moves whose EffectiveUtc is at or before <paramref name="asOf"/>.</summary>
    Task<List<SeniorityMove>> GetApprovedDueAsync(DateTime asOf, CancellationToken ct = default);
    /// <summary>Returns the earliest EffectiveUtc among all Approved moves, or null if none exist.</summary>
    Task<DateTime?> GetNextApprovedEffectiveUtcAsync(CancellationToken ct = default);
    /// <summary>Returns all pending moves targeting the same position as <paramref name="targetPositionCtrlNbr"/>, excluding move <paramref name="excludeCtrlNbr"/>.</summary>
    Task<List<SeniorityMove>> GetPendingByTargetPositionAsync(ControlNumber targetPositionCtrlNbr, ControlNumber excludeCtrlNbr, CancellationToken ct = default);
}

public interface ICraftOperationsPolicyRepository : IRepository<CraftOperationsPolicy>
{
    Task<CraftOperationsPolicy?> GetByCraftAsync(ControlNumber craftCtrlNbr, CancellationToken ct = default);
}
