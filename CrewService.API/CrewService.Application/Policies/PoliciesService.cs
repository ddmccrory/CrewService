using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Policies;

public sealed class PoliciesService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<CraftDisplacementPolicy> GetOrUpsertDisplacementPolicyAsync(
        long craftCtrlNbr, int windowHours, string seniorityBasis, string defaultAction,
        string? eligibilitySelectorJson, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.CraftDisplacementPolicies.GetByCraftAsync(ControlNumber.Create(craftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(windowHours, seniorityBasis, defaultAction, eligibilitySelectorJson);
            await uow.CraftDisplacementPolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var policy = CraftDisplacementPolicy.Create(craftCtrlNbr, windowHours, seniorityBasis, defaultAction, eligibilitySelectorJson);
        await uow.CraftDisplacementPolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<CraftDisplacementPolicy> GetDisplacementPolicyAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.CraftDisplacementPolicies.GetByCraftAsync(craftCtrlNbr)
            ?? throw new KeyNotFoundException($"Displacement policy for craft {craftCtrlNbr} not found.");
    }

    public async Task<BulletinPolicy> GetOrUpsertBulletinPolicyAsync(
        long craftCtrlNbr, int bidWindowHours, bool forcedAssignmentEnabled, string forcedAssignmentBasis,
        CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.BulletinPolicies.GetByCraftAsync(ControlNumber.Create(craftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(bidWindowHours, forcedAssignmentEnabled, forcedAssignmentBasis);
            await uow.BulletinPolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var policy = BulletinPolicy.Create(craftCtrlNbr, bidWindowHours, forcedAssignmentEnabled, forcedAssignmentBasis);
        await uow.BulletinPolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<BulletinPolicy> GetBulletinPolicyAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.BulletinPolicies.GetByCraftAsync(craftCtrlNbr)
            ?? throw new KeyNotFoundException($"Bulletin policy for craft {craftCtrlNbr} not found.");
    }

    public async Task<SeniorityMovePolicy> GetOrUpsertSeniorityMovePolicyAsync(
        long craftCtrlNbr, int eligibilityDays, string seniorityBasis, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.SeniorityMovePolicies.GetByCraftAsync(ControlNumber.Create(craftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(eligibilityDays, seniorityBasis);
            await uow.SeniorityMovePolicies.UpdateAsync(existing, ct);
            await uow.CommitAsync(ct);
            return existing;
        }
        var policy = SeniorityMovePolicy.Create(craftCtrlNbr, eligibilityDays, seniorityBasis);
        await uow.SeniorityMovePolicies.AddAsync(policy, ct);
        await uow.CommitAsync(ct);
        return policy;
    }

    public async Task<SeniorityMovePolicy> GetSeniorityMovePolicyAsync(
        ControlNumber craftCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityMovePolicies.GetByCraftAsync(craftCtrlNbr)
            ?? throw new KeyNotFoundException($"Seniority move policy for craft {craftCtrlNbr} not found.");
    }

    public async Task<SeniorityMove> ExerciseSeniorityMoveAsync(
        long employeeCtrlNbr, long craftCtrlNbr, long targetPositionCtrlNbr,
        long? displacedEmployeeCtrlNbr, int daysOnCurrentPosition, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var move = SeniorityMove.Create(employeeCtrlNbr, craftCtrlNbr, targetPositionCtrlNbr,
            displacedEmployeeCtrlNbr == null || displacedEmployeeCtrlNbr == 0 ? null : displacedEmployeeCtrlNbr,
            daysOnCurrentPosition);
        await uow.SeniorityMoves.AddAsync(move, ct);
        await uow.CommitAsync(ct);
        return move;
    }

    public async Task<IReadOnlyList<SeniorityMove>> GetSeniorityMovesByEmployeeAsync(
        ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.SeniorityMoves.GetByEmployeeAsync(employeeCtrlNbr);
    }
}
