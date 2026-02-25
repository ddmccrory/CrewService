using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class PoliciesService(
    ICraftDisplacementPolicyRepository policyRepository,
    IBulletinPolicyRepository bulletinPolicyRepository,
    ISeniorityMovePolicyRepository seniorityMovePolicyRepository,
    ISeniorityMoveRepository seniorityMoveRepository) : PoliciesSrvc.PoliciesSrvcBase
{
    public override async Task<DisplacementPolicyResponse> GetDisplacementPolicy(GetDisplacementPolicyRequest request, ServerCallContext context)
    {
        var policy = await policyRepository.GetByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Displacement policy for craft {request.CraftCtrlNbr} not found."));
        return MapPolicy(policy);
    }

    public override async Task<DisplacementPolicyResponse> UpsertDisplacementPolicy(UpsertDisplacementPolicyRequest request, ServerCallContext context)
    {
        var existing = await policyRepository.GetByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(request.WindowHours, request.SeniorityBasis, request.DefaultAction, request.EligibilitySelectorJson);
            await policyRepository.UpdateAsync(existing);
            return MapPolicy(existing);
        }

        var policy = CraftDisplacementPolicy.Create(request.CraftCtrlNbr, request.WindowHours, request.SeniorityBasis, request.DefaultAction, request.EligibilitySelectorJson);
        await policyRepository.AddAsync(policy);
        return MapPolicy(policy);
    }

    private static DisplacementPolicyResponse MapPolicy(CraftDisplacementPolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        WindowHours = p.WindowHours,
        SeniorityBasis = p.SeniorityBasis,
        DefaultAction = p.DefaultAction,
        EligibilitySelectorJson = p.EligibilitySelectorJson ?? string.Empty
    };

    // Bulletin Policy

    public override async Task<BulletinPolicyResponse> GetBulletinPolicy(GetBulletinPolicyRequest request, ServerCallContext context)
    {
        var policy = await bulletinPolicyRepository.GetByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Bulletin policy for craft {request.CraftCtrlNbr} not found."));
        return MapBulletinPolicy(policy);
    }

    public override async Task<BulletinPolicyResponse> UpsertBulletinPolicy(UpsertBulletinPolicyRequest request, ServerCallContext context)
    {
        var existing = await bulletinPolicyRepository.GetByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(request.BidWindowHours, request.ForcedAssignmentEnabled, request.ForcedAssignmentBasis);
            await bulletinPolicyRepository.UpdateAsync(existing);
            return MapBulletinPolicy(existing);
        }

        var policy = BulletinPolicy.Create(request.CraftCtrlNbr, request.BidWindowHours, request.ForcedAssignmentEnabled, request.ForcedAssignmentBasis);
        await bulletinPolicyRepository.AddAsync(policy);
        return MapBulletinPolicy(policy);
    }

    private static BulletinPolicyResponse MapBulletinPolicy(BulletinPolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        BidWindowHours = p.BidWindowHours,
        ForcedAssignmentEnabled = p.ForcedAssignmentEnabled,
        ForcedAssignmentBasis = p.ForcedAssignmentBasis
    };

    // Seniority Move Policy

    public override async Task<SeniorityMovePolicyResponse> GetSeniorityMovePolicy(GetSeniorityMovePolicyRequest request, ServerCallContext context)
    {
        var policy = await seniorityMovePolicyRepository.GetByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Seniority move policy for craft {request.CraftCtrlNbr} not found."));
        return MapSeniorityMovePolicy(policy);
    }

    public override async Task<SeniorityMovePolicyResponse> UpsertSeniorityMovePolicy(UpsertSeniorityMovePolicyRequest request, ServerCallContext context)
    {
        var existing = await seniorityMovePolicyRepository.GetByCraftAsync(ControlNumber.Create(request.CraftCtrlNbr));
        if (existing is not null)
        {
            existing.Update(request.EligibilityDays, request.SeniorityBasis);
            await seniorityMovePolicyRepository.UpdateAsync(existing);
            return MapSeniorityMovePolicy(existing);
        }

        var policy = SeniorityMovePolicy.Create(request.CraftCtrlNbr, request.EligibilityDays, request.SeniorityBasis);
        await seniorityMovePolicyRepository.AddAsync(policy);
        return MapSeniorityMovePolicy(policy);
    }

    private static SeniorityMovePolicyResponse MapSeniorityMovePolicy(SeniorityMovePolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        EligibilityDays = p.EligibilityDays,
        SeniorityBasis = p.SeniorityBasis
    };

    // Seniority Move

    public override async Task<SeniorityMoveResponse> ExerciseSeniorityMove(ExerciseSeniorityMoveRequest request, ServerCallContext context)
    {
        var move = SeniorityMove.Create(request.EmployeeCtrlNbr, request.CraftCtrlNbr,
            request.TargetPositionCtrlNbr, request.DisplacedEmployeeCtrlNbr == 0 ? null : request.DisplacedEmployeeCtrlNbr,
            request.DaysOnCurrentPosition);
        await seniorityMoveRepository.AddAsync(move);
        return MapSeniorityMove(move);
    }

    public override async Task<GetSeniorityMovesResponse> GetSeniorityMovesByEmployee(GetSeniorityMovesByEmployeeRequest request, ServerCallContext context)
    {
        var moves = await seniorityMoveRepository.GetByEmployeeAsync(ControlNumber.Create(request.EmployeeCtrlNbr));
        var response = new GetSeniorityMovesResponse { TotalCount = moves.Count };
        foreach (var m in moves) response.Moves.Add(MapSeniorityMove(m));
        return response;
    }

    private static SeniorityMoveResponse MapSeniorityMove(SeniorityMove m) => new()
    {
        CtrlNbr = m.CtrlNbr.Value,
        EmployeeCtrlNbr = m.EmployeeCtrlNbr.Value,
        CraftCtrlNbr = m.CraftCtrlNbr.Value,
        TargetPositionCtrlNbr = m.TargetPositionCtrlNbr.Value,
        DisplacedEmployeeCtrlNbr = m.DisplacedEmployeeCtrlNbr?.Value ?? 0,
        ExercisedUtc = m.ExercisedUtc.ToString("O"),
        DaysOnCurrentPosition = m.DaysOnCurrentPosition
    };
}
