using CrewService.Domain.Modules.Policies;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class PoliciesService(
    ICraftDisplacementPolicyRepository policyRepository) : PoliciesSrvc.PoliciesSrvcBase
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
}
