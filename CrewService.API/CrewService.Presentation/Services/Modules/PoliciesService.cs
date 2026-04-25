using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class PoliciesService(IServiceProvider serviceProvider) : PoliciesSrvc.PoliciesSrvcBase
{
    public override async Task<DisplacementPolicyResponse> GetDisplacementPolicy(GetDisplacementPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var policy = await svc.GetDisplacementPolicyAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken);
            return MapPolicy(policy);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<DisplacementPolicyResponse> UpsertDisplacementPolicy(UpsertDisplacementPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var policy = await svc.GetOrUpsertDisplacementPolicyAsync(
            request.CraftCtrlNbr, request.WindowHours, request.SeniorityBasis,
            request.DefaultAction, request.EligibilitySelectorJson, context.CancellationToken);
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

    public override async Task<BulletinPolicyResponse> GetBulletinPolicy(GetBulletinPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var policy = await svc.GetBulletinPolicyAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken);
            return MapBulletinPolicy(policy);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<BulletinPolicyResponse> UpsertBulletinPolicy(UpsertBulletinPolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var policy = await svc.GetOrUpsertBulletinPolicyAsync(
            request.CraftCtrlNbr, request.BidWindowHours, request.ForcedAssignmentEnabled,
            request.ForcedAssignmentBasis, context.CancellationToken);
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

    public override async Task<SeniorityMovePolicyResponse> GetSeniorityMovePolicy(GetSeniorityMovePolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        try
        {
            var policy = await svc.GetSeniorityMovePolicyAsync(ControlNumber.Create(request.CraftCtrlNbr), context.CancellationToken);
            return MapSeniorityMovePolicy(policy);
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
    }

    public override async Task<SeniorityMovePolicyResponse> UpsertSeniorityMovePolicy(UpsertSeniorityMovePolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var policy = await svc.GetOrUpsertSeniorityMovePolicyAsync(
            request.CraftCtrlNbr, request.EligibilityDays, request.SeniorityBasis, context.CancellationToken);
        return MapSeniorityMovePolicy(policy);
    }

    private static SeniorityMovePolicyResponse MapSeniorityMovePolicy(SeniorityMovePolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        EligibilityDays = p.EligibilityDays,
        SeniorityBasis = p.SeniorityBasis
    };

    public override async Task<SeniorityMoveResponse> ExerciseSeniorityMove(ExerciseSeniorityMoveRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var move = await svc.ExerciseSeniorityMoveAsync(
            request.EmployeeCtrlNbr, request.CraftCtrlNbr, request.TargetPositionCtrlNbr,
            request.DisplacedEmployeeCtrlNbr == 0 ? null : request.DisplacedEmployeeCtrlNbr,
            request.DaysOnCurrentPosition, context.CancellationToken);
        return MapSeniorityMove(move);
    }

    public override async Task<GetSeniorityMovesResponse> GetSeniorityMovesByEmployee(GetSeniorityMovesByEmployeeRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<Application.Policies.PoliciesService>();
        var moves = await svc.GetSeniorityMovesByEmployeeAsync(
            ControlNumber.Create(request.EmployeeCtrlNbr), context.CancellationToken);
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

