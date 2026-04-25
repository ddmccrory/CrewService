using CrewService.Application.Boards;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.ValueObjects;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class BoardsService(IServiceProvider serviceProvider) : BoardsSrvc.BoardsSrvcBase
{
    public override async Task<CascadePolicyResponse> GetCascadePolicy(GetCascadePolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<BoardCascadePolicyService>();
        var policy = await svc.GetByWorkAreaAndCraftAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr), ControlNumber.Create(request.CraftCtrlNbr))
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Cascade policy not found."));
        return MapCascade(policy);
    }

    public override async Task<CascadePolicyResponse> UpsertCascadePolicy(UpsertCascadePolicyRequest request, ServerCallContext context)
    {
        var svc = serviceProvider.GetRequiredService<BoardCascadePolicyService>();
        var policy = await svc.UpsertAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            ControlNumber.Create(request.CraftCtrlNbr),
            request.CascadeMode,
            request.MaxLevels > 0 ? request.MaxLevels : null,
            request.AuxEnabled,
            request.AuxMaxLevels > 0 ? request.AuxMaxLevels : null,
            string.IsNullOrEmpty(request.SelectionStrategy) ? null : request.SelectionStrategy);
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
