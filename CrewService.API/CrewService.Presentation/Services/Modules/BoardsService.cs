using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using Grpc.Core;

namespace CrewService.Presentation.Services.Modules;

public class BoardsService(
    IBoardCascadePolicyRepository cascadeRepository) : BoardsSrvc.BoardsSrvcBase
{
    public override async Task<CascadePolicyResponse> GetCascadePolicy(GetCascadePolicyRequest request, ServerCallContext context)
    {
        var policy = await cascadeRepository.GetByWorkAreaAndCraftAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), ControlNumber.Create(request.CraftCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Cascade policy not found."));
        return MapCascade(policy);
    }

    public override async Task<CascadePolicyResponse> UpsertCascadePolicy(UpsertCascadePolicyRequest request, ServerCallContext context)
    {
        var existing = await cascadeRepository.GetByWorkAreaAndCraftAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), ControlNumber.Create(request.CraftCtrlNbr));
        if (existing is not null)
        {
            await cascadeRepository.DeleteAsync(existing.CtrlNbr);
        }
        var policy = BoardCascadePolicy.Create(
            request.WorkAreaGroupCtrlNbr, request.CraftCtrlNbr,
            request.CascadeMode, request.MaxLevels > 0 ? request.MaxLevels : null,
            request.AuxEnabled, request.AuxMaxLevels > 0 ? request.AuxMaxLevels : null,
            string.IsNullOrEmpty(request.SelectionStrategy) ? null : request.SelectionStrategy);
        await cascadeRepository.AddAsync(policy);
        return MapCascade(policy);
    }

    private static CascadePolicyResponse MapCascade(BoardCascadePolicy p) => new()
    {
        CtrlNbr = p.CtrlNbr.Value,
        WorkAreaGroupCtrlNbr = p.WorkAreaGroupCtrlNbr.Value,
        CraftCtrlNbr = p.CraftCtrlNbr.Value,
        CascadeMode = p.CascadeMode,
        MaxLevels = p.MaxLevels ?? 0,
        AuxEnabled = p.AuxEnabled,
        AuxMaxLevels = p.AuxMaxLevels ?? 0,
        SelectionStrategy = p.SelectionStrategy ?? string.Empty
    };
}
